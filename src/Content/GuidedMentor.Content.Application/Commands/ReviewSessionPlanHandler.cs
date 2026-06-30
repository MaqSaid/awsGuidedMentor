using GuidedMentor.Content.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Content.Application.Commands;

/// <summary>
/// Handles the ReviewSessionPlanCommand by updating the session plan's review status.
/// Implements human oversight capability per ISO 42001 requirement 8.4:
/// - Super_Admin can approve or flag AI-generated session plans
/// - All review actions are audit-logged via the IAdminCommand interface
/// - Flagged plans are marked for attention and can be investigated
/// 
/// Validates: Requirement 21.17 (ISO 42001 — 8.4 Human oversight)
/// </summary>
public sealed class ReviewSessionPlanHandler : IRequestHandler<ReviewSessionPlanCommand, Result>
{
    private readonly ISessionPlanRepository _sessionPlanRepository;
    private readonly ILogger<ReviewSessionPlanHandler> _logger;

    public ReviewSessionPlanHandler(
        ISessionPlanRepository sessionPlanRepository,
        ILogger<ReviewSessionPlanHandler> logger)
    {
        _sessionPlanRepository = sessionPlanRepository ?? throw new ArgumentNullException(nameof(sessionPlanRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(ReviewSessionPlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Admin {AdminId} reviewing session plan for SessionId={SessionId}, Action={Action}",
            request.AdminId, request.SessionId, request.Action);

        if (request.Action == SessionPlanReviewAction.Flag && string.IsNullOrWhiteSpace(request.FlagReason))
        {
            return Result.Failure("A reason is required when flagging a session plan.");
        }

        var reviewStatus = request.Action switch
        {
            SessionPlanReviewAction.Approve => "approved",
            SessionPlanReviewAction.Flag => "flagged",
            _ => throw new ArgumentOutOfRangeException(nameof(request.Action))
        };

        await _sessionPlanRepository.UpdateReviewStatusAsync(
            request.SessionId,
            request.AdminId,
            reviewStatus,
            request.FlagReason,
            cancellationToken);

        _logger.LogInformation(
            "Session plan review completed for SessionId={SessionId}. Status={ReviewStatus}, AdminId={AdminId}",
            request.SessionId, reviewStatus, request.AdminId);

        return Result.Success();
    }
}
