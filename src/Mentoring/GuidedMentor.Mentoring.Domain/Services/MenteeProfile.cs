using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Services;

/// <summary>
/// Represents a mentee's profile data used as input for compatibility scoring.
/// </summary>
public sealed record MenteeProfile(
    string DisplayName,
    AustralianChapter Chapter,
    string City,
    IReadOnlyList<string> Skills,
    int YearsOfExperience,
    PrimaryGoal PrimaryGoal);

/// <summary>
/// Mentee primary goal for mentorship matching.
/// </summary>
public enum PrimaryGoal
{
    CareerTransition,
    SkillDevelopment,
    CertificationPreparation,
    ProjectGuidance
}
