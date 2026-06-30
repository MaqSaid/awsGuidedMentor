using Amazon.Lambda.Core;
using GuidedMentor.BackgroundJobs.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Lambda function triggered daily by EventBridge Scheduler.
/// Checks all mentors who have been Unavailable for more than 90 days
/// and sends a reminder notification asking if they wish to remain or deactivate.
///
/// Requirements: 32.7
/// </summary>
public sealed class MentorAvailabilityReminderFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<MentorAvailabilityReminderFunction> _logger;

    public MentorAvailabilityReminderFunction()
    {
        var services = ServiceProviderFactory.Create();
        _mediator = services.GetRequiredService<IMediator>();
        _logger = services.GetRequiredService<ILogger<MentorAvailabilityReminderFunction>>();
    }

    /// <summary>
    /// Entry point invoked daily at midnight UTC by EventBridge Scheduler.
    /// Dispatches SendMentorAvailabilityReminderCommand via MediatR.
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task HandleAsync(ScheduledEvent scheduledEvent, ILambdaContext context)
    {
        _logger.LogInformation(
            "Mentor 90-day availability reminder check triggered. RequestId: {RequestId}",
            context.AwsRequestId);

        try
        {
            var command = new SendMentorAvailabilityReminderCommand();
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Mentor availability reminder check completed successfully.");
            }
            else
            {
                _logger.LogWarning(
                    "Mentor availability reminder check completed with error: {Error}",
                    result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mentor availability reminder check failed.");
            throw;
        }
    }
}
