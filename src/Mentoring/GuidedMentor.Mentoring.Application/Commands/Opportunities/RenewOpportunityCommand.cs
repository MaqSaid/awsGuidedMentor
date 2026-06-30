using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Renews an expired job posting by extending its expiry by 30 days.
/// Only job-type postings can be renewed.
/// </summary>
public sealed record RenewOpportunityCommand(
    Guid PostingId,
    Guid MentorId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"Opportunity:{PostingId}";
}
