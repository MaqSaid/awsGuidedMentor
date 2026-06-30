using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Aligns a mentoring session to a meetup event.
/// The session date should match the meetup date. Adapts agenda context.
/// </summary>
public sealed record AlignSessionToMeetupCommand(
    Guid SessionId,
    Guid MeetupEventId) : IRequest<Result>;
