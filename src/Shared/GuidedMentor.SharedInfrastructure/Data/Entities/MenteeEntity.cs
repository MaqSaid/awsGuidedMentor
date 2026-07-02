namespace GuidedMentor.SharedInfrastructure.Data.Entities;

/// <summary>
/// Persistence model for the mentees table.
/// </summary>
public sealed class MenteeEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string[] Skills { get; set; } = [];
    public string? ExperienceLevel { get; set; }
    public int YearsOfExperience { get; set; }
    public string? PrimaryGoal { get; set; }
    public string? GoalDescription { get; set; }
    public string? PreferredDuration { get; set; }
    public string Availability { get; set; } = "{}";
    public string? CommunicationPreference { get; set; }
    public string? ResumeUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
