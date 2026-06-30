using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Handles withdrawing a mentor's attendance from a meetup event.
/// </summary>
public sealed class WithdrawMeetupAttendanceHandler : IRequestHandler<WithdrawMeetupAttendanceCommand, Result>
{
    private readonly IMeetupEventRepository _meetupRepository;

    public WithdrawMeetupAttendanceHandler(IMeetupEventRepository meetupRepository)
    {
        _meetupRepository = meetupRepository;
    }

    public async Task<Result> Handle(
        WithdrawMeetupAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var meetupEvent = await _meetupRepository.GetByIdAsync(
            new MeetupEventId(request.EventId), cancellationToken);

        if (meetupEvent is null)
            return Result.Failure("Meetup event not found.");

        var result = meetupEvent.WithdrawAttendance(new MentorId(request.MentorId));
        if (result.IsFailure)
            return result;

        await _meetupRepository.SaveAsync(meetupEvent, cancellationToken);

        return Result.Success();
    }
}
