using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Archives an opportunity posting, removing it from public visibility.
/// </summary>
public sealed record ArchiveOpportunityCommand(
    Guid PostingId,
    Guid MentorId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"Opportunity:{PostingId}";
}
