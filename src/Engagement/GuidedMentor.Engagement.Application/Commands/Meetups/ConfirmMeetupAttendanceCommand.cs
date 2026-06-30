using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Confirms a mentor's attendance at a meetup event.
/// </summary>
public sealed record ConfirmMeetupAttendanceCommand(
    Guid EventId,
    Guid MentorId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"MeetupEvent:{EventId}:Attendance";
}
