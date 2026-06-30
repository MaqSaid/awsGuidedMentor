namespace GuidedMentor.Identity.Application.DTOs.Onboarding;

/// <summary>
/// Step 2 (Expertise) data for mentor onboarding.
/// </summary>
public sealed record MentorStep2Data(
    IReadOnlyList<string> ExpertiseAreas,
    int YearsOfExperience,
    IReadOnlyList<string> Certifications,
    IReadOnlyList<string> Topics);
