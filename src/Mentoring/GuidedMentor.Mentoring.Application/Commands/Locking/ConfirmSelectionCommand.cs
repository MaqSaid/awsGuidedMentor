using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Locking;

/// <summary>
/// Confirms the mentor selection after a lock is held.
/// Creates a pending session record and notifies the mentor.
/// Consumes (releases) the lock after session creation.
/// </summary>
public sealed record ConfirmSelectionCommand(Guid LockId, Guid MenteeId, Guid MentorId) : IRequest<Result<ConfirmSelectionResponse>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MenteeId;
    string IAuditableCommand.AuditResourceId => $"Session:Lock:{LockId}:Mentor:{MentorId}";
}
