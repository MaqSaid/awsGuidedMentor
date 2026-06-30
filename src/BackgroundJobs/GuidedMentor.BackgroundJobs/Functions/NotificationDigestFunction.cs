using Amazon.Lambda.Core;
using GuidedMentor.BackgroundJobs.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Lambda function triggered daily at 9:00 AM AEST (23:00 UTC previous day) by EventBridge Scheduler.
/// Compiles unread notifications from the past 24 hours and sends a daily digest email/push
/// to users who have unread notifications.
///
/// Requirements: 20.7
/// </summary>
public sealed class NotificationDigestFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotificationDigestFunction> _logger;

    public NotificationDigestFunction()
    {
        var services = ServiceProviderFactory.Create();
        _mediator = services.GetRequiredService<IMediator>();
        _logger = services.GetRequiredService<ILogger<NotificationDigestFunction>>();
    }

    /// <summary>
    /// Entry point invoked daily at 9 AM AEST by EventBridge Scheduler.
    /// Dispatches SendNotificationDigestCommand via MediatR.
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task HandleAsync(ScheduledEvent scheduledEvent, ILambdaContext context)
    {
        _logger.LogInformation(
            "Notification digest triggered at {Time}. RequestId: {RequestId}",
            DateTime.UtcNow,
            context.AwsRequestId);

        try
        {
            var command = new SendNotificationDigestCommand();
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Notification digest sent successfully.");
            }
            else
            {
                _logger.LogWarning("Notification digest completed with error: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification digest processing failed.");
            throw;
        }
    }
}
