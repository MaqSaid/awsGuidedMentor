using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Content.Application.Commands;

/// <summary>
/// Command for Super_Admin to review and optionally flag an AI-generated session plan.
/// Implements human oversight per ISO 42001 requirement 8.4.
/// 
/// Validates: Requirement 21.17 (ISO 42001 — 8.4 Human oversight)
/// </summary>
public sealed record ReviewSessionPlanCommand(
    Guid AdminId,
    Guid SessionId,
    SessionPlanReviewAction Action,
    string? FlagReason) : IRequest<Result>, IAdminCommand
{
    Guid IAuditableCommand.UserId => AdminId;
    string IAuditableCommand.AuditResourceId => $"Session:{SessionId}:Plan:Review";
    Guid IAdminCommand.AdminId => AdminId;
    string IAdminCommand.AuditTarget => $"Session:{SessionId}";
    string IAdminCommand.AuditReason => Action == SessionPlanReviewAction.Flag
        ? FlagReason ?? "Flagged for review"
        : "Approved";
}

/// <summary>
/// Actions an admin can take when reviewing an AI-generated session plan.
/// </summary>
public enum SessionPlanReviewAction
{
    /// <summary>Admin approves the AI-generated plan — no issues found.</summary>
    Approve,

    /// <summary>Admin flags the plan as problematic — requires attention or removal.</summary>
    Flag
}
