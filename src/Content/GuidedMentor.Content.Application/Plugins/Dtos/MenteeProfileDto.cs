namespace GuidedMentor.Content.Application.Plugins.Dtos;

/// <summary>
/// DTO representing a mentee's profile data for session plan generation prompts.
/// </summary>
public sealed record MenteeProfileDto(
    string DisplayName,
    string Chapter,
    IReadOnlyList<string> Skills,
    int YearsOfExperience,
    string ExperienceLevel,
    string PrimaryGoal,
    string GoalDescription,
    string PreferredDuration);
