namespace GuidedMentor.Engagement.Application.Interfaces;

/// <summary>
/// Publishes meetup-related notifications via the Notification_Service (AppSync).
/// </summary>
public interface IMeetupNotificationPublisher
{
    /// <summary>
    /// Notifies all mentor-mentee pairs with sessions aligned to a cancelled meetup event.
    /// Prompts mentors to reschedule to another meetup or independent time slot.
    /// </summary>
    Task NotifyMeetupCancelledAsync(
        Guid meetupEventId,
        string meetupTitle,
        IReadOnlyList<Guid> affectedSessionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a 24-hour reminder notification to both mentor and mentee for a meetup-aligned session.
    /// Includes meetup venue details and session agenda summary.
    /// </summary>
    Task SendMeetupSessionReminderAsync(
        Guid sessionId,
        Guid mentorId,
        Guid menteeId,
        Guid meetupEventId,
        string venueName,
        string venueAddress,
        string sessionTitle,
        CancellationToken cancellationToken = default);
}
