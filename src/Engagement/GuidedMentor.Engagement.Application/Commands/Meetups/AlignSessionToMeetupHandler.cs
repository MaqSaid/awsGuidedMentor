using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Handles aligning a mentoring session with a meetup event.
/// Associates the session with the meetup for scheduling and agenda adaptation.
/// </summary>
public sealed class AlignSessionToMeetupHandler : IRequestHandler<AlignSessionToMeetupCommand, Result>
{
    private readonly IMeetupEventRepository _meetupRepository;

    public AlignSessionToMeetupHandler(IMeetupEventRepository meetupRepository)
    {
        _meetupRepository = meetupRepository;
    }

    public async Task<Result> Handle(
        AlignSessionToMeetupCommand request,
        CancellationToken cancellationToken)
    {
        var meetupEvent = await _meetupRepository.GetByIdAsync(
            new MeetupEventId(request.MeetupEventId), cancellationToken);

        if (meetupEvent is null)
            return Result.Failure("Meetup event not found.");

        if (meetupEvent.IsCancelled)
            return Result.Failure("Cannot align session to a cancelled meetup event.");

        if (meetupEvent.EventDate.Date < DateTime.UtcNow.Date)
            return Result.Failure("Cannot align session to a past meetup event.");

        await _meetupRepository.AlignSessionAsync(
            request.SessionId,
            meetupEvent.Id,
            cancellationToken);

        return Result.Success();
    }
}
