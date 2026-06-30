using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.RoleSelection;

/// <summary>
/// Sets the initial role for a user after first authentication.
/// Routes the user to the corresponding onboarding flow.
/// </summary>
public sealed record SetRoleCommand(Guid UserId, Role Role) : IRequest<Result<SetRoleResponse>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => UserId;
    string IAuditableCommand.AuditResourceId => $"User:{UserId}:Role:{Role}";
}
