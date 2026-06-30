using GuidedMentor.Content.Application.Interfaces;
using GuidedMentor.Content.Application.Plugins;
using GuidedMentor.Content.Application.Services;
using GuidedMentor.Content.Domain;
using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace GuidedMentor.Content.Application.Commands;

/// <summary>
/// Handles the GenerateSessionPlanCommand by invoking the Semantic Kernel SessionPlanPlugin,
/// validating the AI-generated output, and persisting it to the Sessions_Table.
/// 
/// Uses Polly v8 resilience pipeline (retry 3x with 2s/4s/8s exponential backoff + circuit breaker)
/// for the Bedrock API call. On total failure after all retries, sets session status to pending_plan
/// and publishes a PlanGenerationFailed event to EventBridge for delayed retry (5-minute schedule).
/// 
/// Validates: Requirements 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 24.5
/// </summary>
public sealed class GenerateSessionPlanHandler : IRequestHandler<GenerateSessionPlanCommand, Result<SessionPlan>>
{
    private readonly SessionPlanPlugin _sessionPlanPlugin;
    private readonly OutputValidator _outputValidator;
    private readonly ISessionPlanRepository _sessionPlanRepository;
    private readonly IContentEventPublisher _eventPublisher;
    private readonly IContentNotificationPublisher _notificationPublisher;
    private readonly IBedrockMetricsPublisher _metricsPublisher;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ILogger<GenerateSessionPlanHandler> _logger;

    /// <summary>
    /// The resilience pipeline name for Bedrock calls, matching the SharedInfrastructure configuration.
    /// </summary>
    internal const string BedrockPipelineName = "bedrock";

    public GenerateSessionPlanHandler(
        SessionPlanPlugin sessionPlanPlugin,
        OutputValidator outputValidator,
        ISessionPlanRepository sessionPlanRepository,
        IContentEventPublisher eventPublisher,
        IContentNotificationPublisher notificationPublisher,
        IBedrockMetricsPublisher metricsPublisher,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        ILogger<GenerateSessionPlanHandler> logger)
    {
        _sessionPlanPlugin = sessionPlanPlugin ?? throw new ArgumentNullException(nameof(sessionPlanPlugin));
        _outputValidator = outputValidator ?? throw new ArgumentNullException(nameof(outputValidator));
        _sessionPlanRepository = sessionPlanRepository ?? throw new ArgumentNullException(nameof(sessionPlanRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _metricsPublisher = metricsPublisher ?? throw new ArgumentNullException(nameof(metricsPublisher));
        _resiliencePipeline = resiliencePipelineProvider.GetPipeline(BedrockPipelineName);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<SessionPlan>> Handle(
        GenerateSessionPlanCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting session plan generation for SessionId={SessionId}, MenteeId={MenteeId}, MentorId={MentorId}",
            request.SessionId, request.MenteeId, request.MentorId);

        try
        {
            // Execute the plan generation through the Polly resilience pipeline
            // (retry 3x: 2s, 4s, 8s exponential backoff + circuit breaker: 5 failures/30s → 60s break)
            var plan = await _resiliencePipeline.ExecuteAsync(
                async ct => await GeneratePlanWithValidationAsync(request, ct),
                cancellationToken);

            if (plan is null)
            {
                // All internal plugin attempts returned invalid plans — treat as failure
                return await HandleGenerationFailureAsync(request, "All generation attempts produced invalid plans.", cancellationToken);
            }

            // Persist the validated plan to the Sessions_Table (includes model version for ISO 42001)
            await _sessionPlanRepository.SavePlanAsync(
                request.SessionId, plan, _sessionPlanPlugin.LastModelVersion, cancellationToken);

            _logger.LogInformation(
                "Session plan persisted successfully for SessionId={SessionId}",
                request.SessionId);

            // Notify both parties of success
            await _notificationPublisher.NotifySessionPlanReadyAsync(
                request.MenteeId,
                request.MentorId,
                request.SessionId,
                plan.SessionTitle,
                cancellationToken);

            return Result<SessionPlan>.Success(plan);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Session plan generation cancelled for SessionId={SessionId}",
                request.SessionId);
            throw;
        }
        catch (Exception ex)
        {
            // Polly pipeline exhausted all retries + circuit breaker tripped
            _logger.LogError(ex,
                "Session plan generation failed after all resilience attempts for SessionId={SessionId}",
                request.SessionId);

            return await HandleGenerationFailureAsync(request, ex.Message, cancellationToken);
        }
    }

    /// <summary>
    /// Generates and validates a session plan. This method is wrapped by the Polly pipeline
    /// so that transient failures in the Bedrock API call trigger automatic retries.
    /// </summary>
    private async Task<SessionPlan?> GeneratePlanWithValidationAsync(
        GenerateSessionPlanCommand request,
        CancellationToken cancellationToken)
    {
        // Invoke the Semantic Kernel plugin to call Bedrock
        var plan = await _sessionPlanPlugin.GeneratePlanAsync(
            request.MenteeProfile,
            request.MentorProfile,
            request.MenteeProfile.GoalDescription,
            cancellationToken);

        if (plan is null)
        {
            _logger.LogWarning(
                "SessionPlanPlugin returned null for SessionId={SessionId}",
                request.SessionId);
            return null;
        }

        // Log token usage as CloudWatch metrics (best-effort, don't fail the pipeline)
        await PublishTokenMetricsSafeAsync(request.SessionId, cancellationToken);

        // Validate AI output: schema conformance, no PII, no harmful content
        var validationResult = _outputValidator.Validate(plan);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Output validation failed for SessionId={SessionId}. Violations: {Violations}",
                request.SessionId,
                string.Join("; ", validationResult.Violations));

            // Throw to trigger Polly retry — the response failed validation
            throw new SessionPlanValidationException(
                $"Session plan output validation failed: {string.Join("; ", validationResult.Violations)}");
        }

        _logger.LogInformation(
            "Session plan generated and validated for SessionId={SessionId}. Title=\"{Title}\"",
            request.SessionId, plan.SessionTitle);

        return plan;
    }

    /// <summary>
    /// Handles the failure case when all retries are exhausted.
    /// Sets session status to pending_plan and publishes PlanGenerationFailed event
    /// to EventBridge for delayed retry (5-minute schedule).
    /// </summary>
    private async Task<Result<SessionPlan>> HandleGenerationFailureAsync(
        GenerateSessionPlanCommand request,
        string reason,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Handling generation failure for SessionId={SessionId}. Setting status to pending_plan. Reason: {Reason}",
            request.SessionId, reason);

        // Set session status to pending_plan
        await _sessionPlanRepository.SetPendingPlanStatusAsync(request.SessionId, cancellationToken);

        // Publish PlanGenerationFailed event to EventBridge with 5-minute delay
        await _eventPublisher.PublishPlanGenerationFailedAsync(
            request.SessionId,
            request.MenteeId,
            request.MentorId,
            reason,
            cancellationToken);

        // Notify both parties of graceful degradation
        await _notificationPublisher.NotifyPlanGenerationDelayedAsync(
            request.MenteeId,
            request.MentorId,
            request.SessionId,
            cancellationToken);

        return Result<SessionPlan>.Failure(
            "Session plan generation failed after all retry attempts. " +
            "The session has been placed in pending-plan state and a retry has been scheduled.");
    }

    /// <summary>
    /// Best-effort token usage metric publishing. Failures here should not break the pipeline.
    /// Extracts actual token counts from the SessionPlanPlugin's last invocation.
    /// </summary>
    private async Task PublishTokenMetricsSafeAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            await _metricsPublisher.PublishTokenUsageAsync(
                inputTokens: _sessionPlanPlugin.LastInputTokens,
                outputTokens: _sessionPlanPlugin.LastOutputTokens,
                sessionId,
                cancellationToken);

            _logger.LogInformation(
                "Published Bedrock token metrics for SessionId={SessionId}: Input={InputTokens}, Output={OutputTokens}",
                sessionId, _sessionPlanPlugin.LastInputTokens, _sessionPlanPlugin.LastOutputTokens);
        }
        catch (Exception ex)
        {
            // Best-effort: don't fail the pipeline for metrics
            _logger.LogWarning(ex,
                "Failed to publish token usage metrics for SessionId={SessionId}",
                sessionId);
        }
    }
}
