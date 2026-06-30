using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Triggers 24-hour reminder notifications for all meetup-aligned sessions
/// whose meetup event is tomorrow. Invoked by a scheduled EventBridge rule.
/// </summary>
public sealed record SendMeetupSessionRemindersCommand(
    AustralianChapter Chapter) : IRequest<Result<int>>;
