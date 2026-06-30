namespace GuidedMentor.Content.Application.Plugins.Dtos;

/// <summary>
/// DTO representing a mentor's profile data for session plan generation prompts.
/// </summary>
public sealed record MentorProfileDto(
    string DisplayName,
    string Chapter,
    IReadOnlyList<string> ExpertiseAreas,
    IReadOnlyList<string> Topics,
    int YearsOfExperience,
    string ProfessionalTitle,
    string CompanyName);
