using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Handles cancelling a meetup event.
/// Validates chapter_lead ownership, identifies aligned sessions, and notifies affected pairs.
/// </summary>
public sealed class CancelMeetupEventHandler : IRequestHandler<CancelMeetupEventCommand, Result>
{
    private readonly IMeetupEventRepository _meetupRepository;
    private readonly IChapterLeadValidator _chapterLeadValidator;
    private readonly IMeetupNotificationPublisher _notificationPublisher;

    public CancelMeetupEventHandler(
        IMeetupEventRepository meetupRepository,
        IChapterLeadValidator chapterLeadValidator,
        IMeetupNotificationPublisher notificationPublisher)
    {
        _meetupRepository = meetupRepository;
        _chapterLeadValidator = chapterLeadValidator;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result> Handle(
        CancelMeetupEventCommand request,
        CancellationToken cancellationToken)
    {
        var meetupEvent = await _meetupRepository.GetByIdAsync(
            new MeetupEventId(request.EventId), cancellationToken);

        if (meetupEvent is null)
            return Result.Failure("Meetup event not found.");

        // Validate chapter_lead owns this event
        var isChapterLead = await _chapterLeadValidator.IsChapterLeadAsync(
            request.ChapterLeadId, meetupEvent.Chapter, cancellationToken);

        if (!isChapterLead)
            return Result.Failure("Only chapter leads can cancel meetup events for their chapter.");

        // Cancel the event (domain validates ownership by CreatedBy)
        var cancelResult = meetupEvent.Cancel(new UserId(request.ChapterLeadId));
        if (cancelResult.IsFailure)
            return cancelResult;

        await _meetupRepository.SaveAsync(meetupEvent, cancellationToken);

        // Identify aligned sessions and notify affected pairs
        var affectedSessionIds = await _meetupRepository.GetAlignedSessionIdsAsync(
            meetupEvent.Id, cancellationToken);

        if (affectedSessionIds.Count > 0)
        {
            await _notificationPublisher.NotifyMeetupCancelledAsync(
                meetupEvent.Id.Value,
                meetupEvent.Title,
                affectedSessionIds,
                cancellationToken);
        }

        return Result.Success();
    }
}
