using GuidedMentor.Engagement.Domain.Entities;

namespace GuidedMentor.Engagement.Application.Interfaces;

/// <summary>
/// Publishes real-time notifications via AWS AppSync GraphQL subscriptions.
/// Delivery target is &lt; 5 seconds from notification creation.
/// </summary>
public interface IAppSyncNotificationPublisher
{
    /// <summary>
    /// Pushes a notification to the recipient's AppSync subscription channel.
    /// </summary>
    Task PublishAsync(Notification notification, CancellationToken cancellationToken = default);
}
