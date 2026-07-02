namespace GuidedMentor.SharedInfrastructure.Data.Entities;

/// <summary>
/// Persistence model for the mentors table.
/// </summary>
public sealed class MentorEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string[] ExpertiseAreas { get; set; } = [];
    public string[] Certifications { get; set; } = [];
    public string[] Topics { get; set; } = [];
    public int YearsOfExperience { get; set; }
    public int MaxMentees { get; set; } = 3;
    public int ActiveMenteeCount { get; set; }
    public string Availability { get; set; } = "{}";
    public string[] SessionFormats { get; set; } = [];
    public string? ProfessionalTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? Bio { get; set; }
    public string AvailabilityStatus { get; set; } = "available";
    public string? UnavailabilityReason { get; set; }
    public DateTime? ReturnDate { get; set; }
    public DateTime? UnavailableSince { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
