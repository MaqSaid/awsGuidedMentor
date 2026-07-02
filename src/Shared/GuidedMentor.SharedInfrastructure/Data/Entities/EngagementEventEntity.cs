namespace GuidedMentor.SharedInfrastructure.Data.Entities;

/// <summary>
/// Persistence model for the engagement_events table (analytics).
/// </summary>
public sealed class EngagementEventEntity
{
    public Guid Id { get; set; }
    public string UserIdHash { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public string? ActiveRole { get; set; }
    public DateTime CreatedAt { get; set; }
}
