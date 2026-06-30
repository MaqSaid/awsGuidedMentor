using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Services;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Queries.Opportunities;

/// <summary>
/// Browses all active opportunity postings with optional filters.
/// Results are sorted by publishedAt descending and paginated.
/// </summary>
public sealed record BrowseOpportunitiesQuery(
    OpportunityType? TypeFilter,
    string? LocationFilter,
    IReadOnlyList<string>? SkillsFilter,
    ExperienceLevel? ExperienceFilter,
    int Page = 1,
    int PageSize = 12) : IRequest<Result<PagedResult<OpportunityPostingDto>>>;
