using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Engagement.Infrastructure.RealTime;

/// <summary>
/// Placeholder notification publisher. Will be replaced with SignalR hub in production.
/// For now, logs the notification and returns (real-time delivery via SignalR is a future step).
/// </summary>
public sealed class NoOpNotificationPublisher : IAppSyncNotificationPublisher
{
    private readonly ILogger<NoOpNotificationPublisher> _logger;

    public NoOpNotificationPublisher(ILogger<NoOpNotificationPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Notification {NotificationId} for user {RecipientUserId} would be pushed via SignalR",
            notification.Id.Value, notification.RecipientUserId.Value);
        return Task.CompletedTask;
    }
}
