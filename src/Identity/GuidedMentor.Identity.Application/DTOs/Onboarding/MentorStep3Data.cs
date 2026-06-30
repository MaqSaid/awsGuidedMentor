namespace GuidedMentor.Identity.Application.DTOs.Onboarding;

/// <summary>
/// Step 3 (Availability) data for mentor onboarding.
/// </summary>
public sealed record MentorStep3Data(
    int MaxMentees,
    Dictionary<string, List<string>> Availability,
    IReadOnlyList<string> SessionFormats,
    string Bio);
