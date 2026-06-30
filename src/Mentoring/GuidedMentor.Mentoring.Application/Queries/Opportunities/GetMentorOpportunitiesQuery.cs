using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Queries.Opportunities;

/// <summary>
/// Retrieves all opportunity postings for a specific mentor (all statuses).
/// Used on the mentor dashboard to manage their own postings.
/// </summary>
public sealed record GetMentorOpportunitiesQuery(Guid MentorId)
    : IRequest<Result<IReadOnlyList<OpportunityPostingDto>>>;
