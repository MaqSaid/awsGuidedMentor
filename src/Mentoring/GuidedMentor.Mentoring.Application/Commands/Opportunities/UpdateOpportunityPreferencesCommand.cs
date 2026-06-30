using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Updates a mentee's opportunity notification preferences.
/// Controls which types of opportunities trigger notifications and skill-match toggle.
/// </summary>
public sealed record UpdateOpportunityPreferencesCommand(
    Guid MenteeId,
    bool IsEnabled,
    List<OpportunityType> TypePreferences,
    bool SkillMatchEnabled) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MenteeId;
    string IAuditableCommand.AuditResourceId => $"Mentee:{MenteeId}:OpportunityPreferences";
}
