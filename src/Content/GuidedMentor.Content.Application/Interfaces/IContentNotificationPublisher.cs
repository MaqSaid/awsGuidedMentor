namespace GuidedMentor.Content.Application.Interfaces;

/// <summary>
/// Publishes notifications from the Content bounded context to the Engagement Context.
/// Decouples Content from notification delivery mechanism (AppSync subscriptions).
/// </summary>
public interface IContentNotificationPublisher
{
    /// <summary>
    /// Notifies both the mentee and mentor that their session plan is ready.
    /// </summary>
    /// <param name="menteeId">The mentee to notify.</param>
    /// <param name="mentorId">The mentor to notify.</param>
    /// <param name="sessionId">The session whose plan is ready.</param>
    /// <param name="sessionTitle">The generated session title for the notification message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifySessionPlanReadyAsync(
        Guid menteeId,
        Guid mentorId,
        Guid sessionId,
        string sessionTitle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies both parties that plan generation is in progress (graceful degradation).
    /// Sent when all immediate retries are exhausted but async retry is scheduled.
    /// </summary>
    /// <param name="menteeId">The mentee to notify.</param>
    /// <param name="mentorId">The mentor to notify.</param>
    /// <param name="sessionId">The session whose plan generation failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyPlanGenerationDelayedAsync(
        Guid menteeId,
        Guid mentorId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
