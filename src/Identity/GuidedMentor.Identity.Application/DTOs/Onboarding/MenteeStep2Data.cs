namespace GuidedMentor.Identity.Application.DTOs.Onboarding;

/// <summary>
/// Step 2 (Skills) data for mentee onboarding.
/// </summary>
public sealed record MenteeStep2Data(
    IReadOnlyList<string> Skills,
    string ExperienceLevel,
    int YearsOfExperience);
