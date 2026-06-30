using GuidedMentor.Engagement.Application.DTOs;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Queries.Meetups;

/// <summary>
/// Handles retrieving upcoming meetups for a chapter.
/// Filters by chapter, max 3 results, sorted by eventDate ascending, excludes cancelled/past.
/// </summary>
public sealed class GetUpcomingMeetupsHandler
    : IRequestHandler<GetUpcomingMeetupsQuery, Result<IReadOnlyList<MeetupEventDto>>>
{
    private readonly IMeetupEventRepository _meetupRepository;

    public GetUpcomingMeetupsHandler(IMeetupEventRepository meetupRepository)
    {
        _meetupRepository = meetupRepository;
    }

    public async Task<Result<IReadOnlyList<MeetupEventDto>>> Handle(
        GetUpcomingMeetupsQuery request,
        CancellationToken cancellationToken)
    {
        var meetups = await _meetupRepository.GetUpcomingByChapterAsync(
            request.Chapter,
            request.Limit,
            cancellationToken);

        var dtos = meetups.Select(MeetupEventDto.FromDomain).ToList();

        return Result<IReadOnlyList<MeetupEventDto>>.Success(dtos);
    }
}
