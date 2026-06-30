using Amazon.Lambda.Core;
using GuidedMentor.Mentoring.Application.Commands.Sessions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Lambda function triggered by EventBridge when a 7-day completion reminder is due.
/// Sends a reminder notification to the non-confirming party (mentor) when the mentee
/// has marked a session as complete but the mentor has not yet confirmed.
///
/// Requirements: 20.2
/// </summary>
public sealed class CompletionReminderFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompletionReminderFunction> _logger;

    public CompletionReminderFunction()
    {
        var services = ServiceProviderFactory.Create();
        _mediator = services.GetRequiredService<IMediator>();
        _logger = services.GetRequiredService<ILogger<CompletionReminderFunction>>();
    }

    /// <summary>
    /// Entry point invoked by EventBridge Scheduler (one-time scheduled event, 7 days after mentee completion).
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task HandleAsync(CompletionReminderEvent reminderEvent, ILambdaContext context)
    {
        _logger.LogInformation(
            "7-day completion reminder triggered for session {SessionId}, recipient {RecipientId}. RequestId: {RequestId}",
            reminderEvent.SessionId,
            reminderEvent.RecipientId,
            context.AwsRequestId);

        try
        {
            var command = new SendCompletionReminderCommand(
                reminderEvent.SessionId,
                reminderEvent.RecipientId);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Completion reminder sent successfully for session {SessionId}.",
                    reminderEvent.SessionId);
            }
            else
            {
                _logger.LogWarning(
                    "Completion reminder skipped for session {SessionId}: {Error}",
                    reminderEvent.SessionId,
                    result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Completion reminder failed for session {SessionId}.",
                reminderEvent.SessionId);
            throw;
        }
    }
}

/// <summary>
/// Event payload from EventBridge Scheduler for the 7-day completion reminder.
/// </summary>
public sealed record CompletionReminderEvent
{
    public Guid SessionId { get; init; }
    public Guid RecipientId { get; init; }
}
