using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Marks a session as complete by the specified user role.
/// Implements the two-party completion flow state machine:
///   - Mentee marks first → status transitions to MenteeCompleted, schedules reminder/escalation
///   - Mentor confirms after mentee → status transitions to Completed, decrements capacity
///   - Mentor-first is rejected
///   - Mentee retraction is rejected
/// </summary>
public sealed record MarkCompleteCommand(Guid SessionId, Guid UserId, Role Role) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => UserId;
    string IAuditableCommand.AuditResourceId => $"Session:{SessionId}";
}
