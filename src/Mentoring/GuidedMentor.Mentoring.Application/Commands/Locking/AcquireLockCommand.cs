using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Locking;

/// <summary>
/// Acquires a 15-minute lock on a mentor for a mentee.
/// Uses DynamoDB conditional writes to prevent race conditions.
/// A mentee can only hold one active lock at a time.
/// </summary>
public sealed record AcquireLockCommand(Guid MenteeId, Guid MentorId) : IRequest<Result<AcquireLockResponse>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MenteeId;
    string IAuditableCommand.AuditResourceId => $"Mentor:{MentorId}:Lock";
}
