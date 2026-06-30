namespace GuidedMentor.Engagement.Domain.Entities;

/// <summary>
/// Types of notifications delivered by the platform.
/// </summary>
public enum NotificationType
{
    RequestSent,
    RequestAccepted,
    RequestDeclined,
    SessionPlanReady,
    CompletionMarked,
    Reminder
}
