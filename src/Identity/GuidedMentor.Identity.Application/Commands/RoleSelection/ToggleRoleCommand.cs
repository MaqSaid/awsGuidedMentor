using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.RoleSelection;

/// <summary>
/// Toggles the user's active role between Mentor and Mentee.
/// Determines if the new role requires onboarding.
/// </summary>
public sealed record ToggleRoleCommand(Guid UserId) : IRequest<Result<ToggleRoleResponse>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => UserId;
    string IAuditableCommand.AuditResourceId => $"User:{UserId}:RoleToggle";
}
