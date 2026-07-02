namespace GuidedMentor.SharedInfrastructure.Data.Entities;

/// <summary>
/// Persistence model for the opportunities table (jobs, workshops, events, training).
/// </summary>
public sealed class OpportunityEntity
{
    public Guid Id { get; set; }
    public Guid MentorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? OrganisationName { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime? EventDateTime { get; set; }
    public string? EmploymentType { get; set; }
    public string[] RequiredSkills { get; set; } = [];
    public string? RequiredExperience { get; set; }
    public string? ExternalUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
