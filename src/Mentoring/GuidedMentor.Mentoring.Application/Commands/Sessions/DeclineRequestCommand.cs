using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Mentor declines a pending session request.
/// Notifies the mentee, releases the mentor slot, and removes the request from the dashboard.
/// </summary>
public sealed record DeclineRequestCommand(Guid SessionId, Guid MentorId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"Session:{SessionId}";
}
