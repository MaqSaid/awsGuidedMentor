namespace GuidedMentor.SharedInfrastructure.Data.Entities;

/// <summary>
/// Persistence model for the users table. Separate from Domain User entity.
/// </summary>
public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public string? AwsChapter { get; set; }
    public string? City { get; set; }
    public string? ActiveRole { get; set; }
    public string MentorOnboardingStatus { get; set; } = "not_started";
    public string MenteeOnboardingStatus { get; set; } = "not_started";
    public bool IsDisabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
