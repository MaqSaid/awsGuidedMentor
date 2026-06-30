using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Cancels a meetup event. Only the chapter lead who created it can cancel.
/// Notifies all mentor-mentee pairs with sessions aligned to the cancelled event.
/// </summary>
public sealed record CancelMeetupEventCommand(
    Guid EventId,
    Guid ChapterLeadId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => ChapterLeadId;
    string IAuditableCommand.AuditResourceId => $"MeetupEvent:{EventId}";
}
