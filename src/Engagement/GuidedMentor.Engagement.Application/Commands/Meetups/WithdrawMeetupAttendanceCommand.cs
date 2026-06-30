using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Withdraws a mentor's attendance from a meetup event.
/// </summary>
public sealed record WithdrawMeetupAttendanceCommand(
    Guid EventId,
    Guid MentorId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"MeetupEvent:{EventId}:Attendance";
}
