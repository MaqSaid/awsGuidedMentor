namespace GuidedMentor.Mentoring.Application.Interfaces;

/// <summary>
/// Publishes events to Amazon EventBridge for cross-context communication
/// and scheduling future actions (reminders, escalations, plan generation).
/// </summary>
public interface IEventBridgePublisher
{
    /// <summary>
    /// Publishes a session-accepted event to trigger plan generation in the Content context.
    /// </summary>
    Task PublishSessionAcceptedAsync(
        Guid sessionId,
        Guid menteeId,
        Guid mentorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a session-completed event to EventBridge for analytics and downstream processing.
    /// </summary>
    Task PublishSessionCompletedAsync(
        Guid sessionId,
        Guid menteeId,
        Guid mentorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a 7-day reminder for the non-confirming party using EventBridge Scheduler.
    /// </summary>
    /// <param name="sessionId">The session awaiting confirmation.</param>
    /// <param name="recipientId">The user to remind (mentor).</param>
    /// <param name="fireAt">When to send the reminder (7 days from mentee completion).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ScheduleCompletionReminderAsync(
        Guid sessionId,
        Guid recipientId,
        DateTime fireAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a 14-day escalation to unresolved status using EventBridge Scheduler.
    /// </summary>
    /// <param name="sessionId">The session to escalate.</param>
    /// <param name="fireAt">When to escalate (14 days from mentee completion).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ScheduleEscalationAsync(
        Guid sessionId,
        DateTime fireAt,
        CancellationToken cancellationToken = default);
}
