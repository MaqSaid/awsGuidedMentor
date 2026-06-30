using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Content.Application.Commands;

/// <summary>
/// Lightweight API-triggered command to request session plan generation.
/// The handler is responsible for fetching mentor/mentee profiles before
/// delegating to the full GenerateSessionPlanCommand pipeline.
/// </summary>
public sealed record RequestSessionPlanGenerationCommand(
    Guid SessionId,
    Guid RequestingUserId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => RequestingUserId;
    string IAuditableCommand.AuditResourceId => $"Session:{SessionId}:PlanRequest";
}
