using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.Mentoring.Domain.Services;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Queries.Opportunities;

/// <summary>
/// Handles browsing opportunity postings with filters and pagination.
/// Only returns active postings, sorted by publishedAt descending.
/// </summary>
public sealed class BrowseOpportunitiesHandler
    : IRequestHandler<BrowseOpportunitiesQuery, Result<PagedResult<OpportunityPostingDto>>>
{
    private readonly IOpportunityRepository _opportunityRepository;

    public BrowseOpportunitiesHandler(IOpportunityRepository opportunityRepository)
    {
        _opportunityRepository = opportunityRepository;
    }

    public async Task<Result<PagedResult<OpportunityPostingDto>>> Handle(
        BrowseOpportunitiesQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _opportunityRepository.BrowseAsync(
            typeFilter: request.TypeFilter,
            locationFilter: request.LocationFilter,
            skillsFilter: request.SkillsFilter,
            experienceFilter: request.ExperienceFilter,
            page: request.Page,
            pageSize: request.PageSize,
            ct: cancellationToken);

        var dtos = items.Select(OpportunityPostingDto.FromDomain).ToList();

        var pagedResult = new PagedResult<OpportunityPostingDto>(
            Items: dtos,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize);

        return Result<PagedResult<OpportunityPostingDto>>.Success(pagedResult);
    }
}
