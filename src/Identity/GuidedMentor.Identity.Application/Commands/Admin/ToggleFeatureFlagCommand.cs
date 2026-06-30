using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Enables or disables a specific platform feature flag without requiring a deployment.
/// Allowed features: ai_help, job_board, meetup_calendar, session_plans.
/// Requires a reason for the audit trail.
/// </summary>
public sealed record ToggleFeatureFlagCommand(
    Guid AdminId,
    string FeatureName,
    bool Enabled,
    string Reason) : IRequest<Result>, IAdminCommand
{
    Guid IAuditableCommand.UserId => AdminId;
    string IAuditableCommand.AuditResourceId => $"Feature:{FeatureName}:{Enabled}";
    Guid IAdminCommand.AdminId => AdminId;
    string IAdminCommand.AuditTarget => $"Feature:{FeatureName}";
    string IAdminCommand.AuditReason => Reason;
}
