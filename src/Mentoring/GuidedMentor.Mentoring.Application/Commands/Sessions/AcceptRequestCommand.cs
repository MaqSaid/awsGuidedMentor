using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Mentor accepts a pending session request.
/// Validates the mentor is below capacity, transitions session to PendingPlan,
/// increments activeMenteeCount, and triggers plan generation via EventBridge.
/// </summary>
public sealed record AcceptRequestCommand(Guid SessionId, Guid MentorId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"Session:{SessionId}";
}
