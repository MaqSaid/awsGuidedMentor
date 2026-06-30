using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Settings;

/// <summary>
/// Updates mentor profile settings. Validates inputs using the same onboarding rules
/// and enforces that maxMentees cannot be reduced below the current active mentee count.
/// </summary>
public sealed record UpdateSettingsCommand(
    Guid MentorId,
    string DisplayName,
    string ProfessionalTitle,
    string CompanyName,
    AustralianChapter Chapter,
    IReadOnlyList<string> ExpertiseAreas,
    int YearsOfExperience,
    IReadOnlyList<string> Certifications,
    IReadOnlyList<string> Topics,
    int MaxMentees,
    IReadOnlyList<string> SessionFormats,
    string Bio) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"Mentor:{MentorId}";
}
