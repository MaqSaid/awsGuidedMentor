using Amazon.Lambda.Core;
using GuidedMentor.Mentoring.Application.Commands.Sessions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Lambda function triggered by EventBridge when the 14-day escalation deadline is reached.
/// Transitions the session to Unresolved status and notifies both mentor and mentee.
///
/// Requirements: 20.5
/// </summary>
public sealed class EscalationFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<EscalationFunction> _logger;

    public EscalationFunction()
    {
        var services = ServiceProviderFactory.Create();
        _mediator = services.GetRequiredService<IMediator>();
        _logger = services.GetRequiredService<ILogger<EscalationFunction>>();
    }

    /// <summary>
    /// Entry point invoked by EventBridge Scheduler (one-time scheduled event, 14 days after mentee completion).
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task HandleAsync(EscalationEvent escalationEvent, ILambdaContext context)
    {
        _logger.LogInformation(
            "14-day escalation triggered for session {SessionId}. RequestId: {RequestId}",
            escalationEvent.SessionId,
            context.AwsRequestId);

        try
        {
            var command = new EscalateSessionCommand(escalationEvent.SessionId);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Session {SessionId} escalated to Unresolved successfully.",
                    escalationEvent.SessionId);
            }
            else
            {
                _logger.LogWarning(
                    "Escalation skipped for session {SessionId}: {Error}",
                    escalationEvent.SessionId,
                    result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Escalation failed for session {SessionId}.",
                escalationEvent.SessionId);
            throw;
        }
    }
}

/// <summary>
/// Event payload from EventBridge Scheduler for the 14-day escalation.
/// </summary>
public sealed record EscalationEvent
{
    public Guid SessionId { get; init; }
}
