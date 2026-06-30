using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Places the platform in or out of maintenance mode.
/// Displays a "Platform temporarily unavailable" page to all non-admin users.
/// Requires a reason for the audit trail.
/// </summary>
public sealed record SetMaintenanceModeCommand(
    Guid AdminId,
    bool Enabled,
    string? EstimatedReturnTime,
    string Reason) : IRequest<Result>, IAdminCommand
{
    Guid IAuditableCommand.UserId => AdminId;
    string IAuditableCommand.AuditResourceId => $"Platform:MaintenanceMode:{Enabled}";
    Guid IAdminCommand.AdminId => AdminId;
    string IAdminCommand.AuditTarget => "Platform:MaintenanceMode";
    string IAdminCommand.AuditReason => Reason;
}
