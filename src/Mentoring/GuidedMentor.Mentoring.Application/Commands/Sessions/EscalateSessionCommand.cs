using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Escalates a session to Unresolved status after 14 days without mentor confirmation.
/// Triggered by EventBridge Scheduler.
/// </summary>
public sealed record EscalateSessionCommand(Guid SessionId) : IRequest<Result>;
