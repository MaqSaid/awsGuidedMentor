using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Sends a 7-day reminder to the mentor who has not yet confirmed session completion.
/// Triggered by EventBridge Scheduler.
/// </summary>
public sealed record SendCompletionReminderCommand(Guid SessionId, Guid RecipientId) : IRequest<Result>;
