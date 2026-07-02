namespace GuidedMentor.SharedInfrastructure.Data.Entities;

/// <summary>
/// Persistence model for the sessions table.
/// </summary>
public sealed class SessionEntity
{
    public Guid Id { get; set; }
    public Guid MenteeId { get; set; }
    public Guid MentorId { get; set; }
    public string Status { get; set; } = "pending_acceptance";
    public string? SessionPlan { get; set; }
    public string ChecklistState { get; set; } = """{"prework": [], "followup": []}""";
    public DateTime? MenteeCompletedAt { get; set; }
    public DateTime? MentorCompletedAt { get; set; }
    public Guid? LockId { get; set; }
    public DateTime? LockExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
