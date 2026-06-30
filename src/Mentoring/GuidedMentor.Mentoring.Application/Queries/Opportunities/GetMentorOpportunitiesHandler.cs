using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Queries.Opportunities;

/// <summary>
/// Handles retrieving all opportunity postings for a mentor.
/// Returns all statuses (active, archived, expired) for the mentor's own management.
/// </summary>
public sealed class GetMentorOpportunitiesHandler
    : IRequestHandler<GetMentorOpportunitiesQuery, Result<IReadOnlyList<OpportunityPostingDto>>>
{
    private readonly IOpportunityRepository _opportunityRepository;

    public GetMentorOpportunitiesHandler(IOpportunityRepository opportunityRepository)
    {
        _opportunityRepository = opportunityRepository;
    }

    public async Task<Result<IReadOnlyList<OpportunityPostingDto>>> Handle(
        GetMentorOpportunitiesQuery request,
        CancellationToken cancellationToken)
    {
        var mentorId = new MentorId(request.MentorId);
        var postings = await _opportunityRepository.GetByMentorAsync(mentorId, cancellationToken);

        var dtos = postings
            .OrderByDescending(p => p.PublishedAt)
            .Select(OpportunityPostingDto.FromDomain)
            .ToList();

        return Result<IReadOnlyList<OpportunityPostingDto>>.Success(dtos);
    }
}
