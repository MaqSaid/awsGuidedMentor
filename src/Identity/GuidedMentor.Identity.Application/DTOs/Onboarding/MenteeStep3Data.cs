namespace GuidedMentor.Identity.Application.DTOs.Onboarding;

/// <summary>
/// Step 3 (Goals) data for mentee onboarding.
/// </summary>
public sealed record MenteeStep3Data(
    string PrimaryGoal,
    string GoalDescription,
    string PreferredDuration);
