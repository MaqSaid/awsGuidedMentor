using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.DTOs;

/// <summary>
/// DTO for mentor settings update. Contains all editable mentor profile fields.
/// Uses same validation rules as onboarding.
/// </summary>
public sealed record MentorSettingsData(
    string FullName,
    string? ProfilePhotoUrl,
    AustralianChapter AwsChapter,
    string City,
    string ProfessionalTitle,
    string CompanyName,
    IReadOnlyList<string> ExpertiseAreas,
    int YearsOfExperience,
    IReadOnlyList<string> Certifications,
    IReadOnlyList<string> Topics,
    int MaxMentees,
    Dictionary<string, List<string>> Availability,
    IReadOnlyList<string> SessionFormats,
    string Bio);

/// <summary>
/// DTO for mentee settings update. Contains all editable mentee profile fields.
/// Uses same validation rules as onboarding.
/// </summary>
public sealed record MenteeSettingsData(
    string FullName,
    string? ProfilePhotoUrl,
    AustralianChapter AwsChapter,
    string City,
    IReadOnlyList<string> Skills,
    string ExperienceLevel,
    int YearsOfExperience,
    string PrimaryGoal,
    string GoalDescription,
    string PreferredDuration,
    Dictionary<string, List<string>> Availability,
    string CommunicationPreference,
    string? ResumeUrl);

/// <summary>
/// Response returned after successful settings update.
/// </summary>
public sealed record UpdateSettingsResponse(
    bool ChapterChanged,
    string Message);
