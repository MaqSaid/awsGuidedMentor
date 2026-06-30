namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Service interface for the daily notification digest.
/// Implementation queries users with unread notifications from the past 24 hours,
/// compiles a summary digest, and dispatches it as a consolidated notification.
/// </summary>
public interface INotificationDigestService
{
    /// <summary>
    /// Sends digest notifications to all users who have unread notifications
    /// accumulated in the last 24 hours.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of digest notifications sent.</returns>
    Task<int> SendDigestsAsync(CancellationToken cancellationToken = default);
}
