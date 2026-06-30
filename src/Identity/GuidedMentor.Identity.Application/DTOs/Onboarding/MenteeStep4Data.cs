namespace GuidedMentor.Identity.Application.DTOs.Onboarding;

/// <summary>
/// Step 4 (Preferences) data for mentee onboarding.
/// </summary>
public sealed record MenteeStep4Data(
    Dictionary<string, List<string>> Availability,
    string CommunicationPreference,
    string? ResumeUrl);
