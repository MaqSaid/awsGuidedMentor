namespace GuidedMentor.Mentoring.Application.Interfaces;

/// <summary>
/// Publishes mentoring notifications to the Engagement Context via EventBridge.
/// Decouples the Mentoring bounded context from the notification delivery mechanism.
/// </summary>
public interface IMentoringNotificationPublisher
{
    /// <summary>
    /// Notifies a mentor that a mentee has selected them and a session is pending acceptance.
    /// </summary>
    Task NotifyMentorOfSelectionAsync(
        Guid mentorId,
        Guid menteeId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a mentee that their session request was accepted by the mentor.
    /// </summary>
    Task NotifyMenteeOfAcceptanceAsync(
        Guid menteeId,
        Guid mentorId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a mentee that their session request was declined by the mentor.
    /// </summary>
    Task NotifyMenteeOfDeclineAsync(
        Guid menteeId,
        Guid mentorId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the mentor that the mentee has marked the session as complete
    /// and requests the mentor to confirm.
    /// </summary>
    Task NotifyMentorOfCompletionMarkAsync(
        Guid mentorId,
        Guid menteeId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies both parties that the session is fully completed.
    /// </summary>
    Task NotifySessionCompletedAsync(
        Guid mentorId,
        Guid menteeId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a 7-day reminder to the non-confirming party (mentor).
    /// </summary>
    Task SendCompletionReminderAsync(
        Guid recipientId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies both parties that the session has been escalated to unresolved
    /// after 14 days without mutual confirmation.
    /// </summary>
    Task NotifyEscalationAsync(
        Guid mentorId,
        Guid menteeId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a mentee that their matched mentor has posted a new opportunity.
    /// </summary>
    Task NotifyMenteeOfNewOpportunityAsync(
        Guid menteeId,
        Guid postingId,
        Guid mentorId,
        Domain.Entities.OpportunityType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a mentee of a skill-matched opportunity from any mentor (≥2 skill overlap, opted-in).
    /// </summary>
    Task NotifyMenteeOfSkillMatchedOpportunityAsync(
        Guid menteeId,
        Guid postingId,
        Guid mentorId,
        Domain.Entities.OpportunityType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a mentor that their job posting has expired, with an option to renew.
    /// </summary>
    Task NotifyMentorOpportunityExpiredWithRenewalAsync(
        Guid mentorId,
        Guid postingId,
        string postingTitle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies a mentor that their non-job posting has expired (auto-archived).
    /// </summary>
    Task NotifyMentorOpportunityExpiredAsync(
        Guid mentorId,
        Guid postingId,
        string postingTitle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a 90-day unavailability reminder to a mentor, asking if they wish
    /// to remain on the platform or deactivate their mentor profile.
    /// </summary>
    Task SendAvailabilityReminderAsync(
        Guid mentorId,
        string displayName,
        DateTime unavailableSince,
        CancellationToken cancellationToken = default);
}
