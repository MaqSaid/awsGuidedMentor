using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Handles creating a new meetup event.
/// Validates the chapter_lead flag before persisting to Meetups_Table.
/// </summary>
public sealed class CreateMeetupEventHandler : IRequestHandler<CreateMeetupEventCommand, Result<Guid>>
{
    private readonly IMeetupEventRepository _meetupRepository;
    private readonly IChapterLeadValidator _chapterLeadValidator;

    public CreateMeetupEventHandler(
        IMeetupEventRepository meetupRepository,
        IChapterLeadValidator chapterLeadValidator)
    {
        _meetupRepository = meetupRepository;
        _chapterLeadValidator = chapterLeadValidator;
    }

    public async Task<Result<Guid>> Handle(
        CreateMeetupEventCommand request,
        CancellationToken cancellationToken)
    {
        // Validate chapter_lead flag
        var isChapterLead = await _chapterLeadValidator.IsChapterLeadAsync(
            request.ChapterLeadId, request.Chapter, cancellationToken);

        if (!isChapterLead)
        {
            return Result<Guid>.Failure(
                "Only chapter leads can create meetup events for their chapter.");
        }

        var meetupEvent = MeetupEvent.Create(
            createdBy: new UserId(request.ChapterLeadId),
            chapter: request.Chapter,
            title: request.Title,
            eventDate: request.EventDate,
            startTime: request.StartTime,
            endTime: request.EndTime,
            venueName: request.VenueName,
            venueAddress: request.VenueAddress,
            eventUrl: request.EventUrl);

        await _meetupRepository.SaveAsync(meetupEvent, cancellationToken);

        return Result<Guid>.Success(meetupEvent.Id.Value);
    }
}
