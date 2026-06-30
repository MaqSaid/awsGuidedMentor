using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Locking;

/// <summary>
/// Releases an active lock, making the mentor available to other mentees.
/// The lock must belong to the specified mentee.
/// </summary>
public sealed record ReleaseLockCommand(Guid LockId, Guid MenteeId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MenteeId;
    string IAuditableCommand.AuditResourceId => $"Lock:{LockId}";
}
