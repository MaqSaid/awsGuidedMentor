namespace GuidedMentor.Engagement.Domain.Entities;

/// <summary>
/// Strongly-typed identifier for the Notification entity.
/// </summary>
public sealed record NotificationId(Guid Value)
{
    public static NotificationId New() => new(Guid.NewGuid());
}
