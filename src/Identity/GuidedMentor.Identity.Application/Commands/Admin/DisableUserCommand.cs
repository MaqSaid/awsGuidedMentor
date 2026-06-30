using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Disables a user account. Requires a reason for the audit trail.
/// Only executable by a Super Admin.
/// </summary>
public sealed record DisableUserCommand(
    Guid AdminId,
    Guid TargetUserId,
    string Reason) : IRequest<Result>, IAdminCommand
{
    Guid IAuditableCommand.UserId => AdminId;
    string IAuditableCommand.AuditResourceId => $"User:{TargetUserId}";
    Guid IAdminCommand.AdminId => AdminId;
    string IAdminCommand.AuditTarget => $"User:{TargetUserId}";
    string IAdminCommand.AuditReason => Reason;
}
