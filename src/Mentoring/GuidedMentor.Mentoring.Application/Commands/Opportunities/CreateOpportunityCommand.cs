using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Creates a new opportunity posting for a mentor.
/// Validates max 5 active postings per mentor (all types combined).
/// Computes expiry as min(publishedAt + 30 days, eventDateTime).
/// </summary>
public sealed record CreateOpportunityCommand(
    Guid MentorId,
    string Title,
    OpportunityType Type,
    string OrganisationName,
    string Description,
    string Location,
    DateTime? EventDateTime,
    EmploymentType? EmploymentType,
    List<string> RequiredSkills,
    ExperienceLevel RequiredExperience,
    string ExternalUrl) : IRequest<Result<Guid>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"Opportunity:New:Mentor:{MentorId}";
}
