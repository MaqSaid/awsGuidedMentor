using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Command to enforce data retention policies across the platform.
/// Scans for inactive user accounts, sends warnings, and deletes expired data.
///
/// Requirements: 21.16 (ISO 27001 — data retention policy)
/// Policy: User data retained 3 years after last activity, deleted on request within 30 days.
/// </summary>
public sealed record DataRetentionCleanupCommand(
    int RetentionYears,
    int GracePeriodDays,
    int DeletionRequestMaxDays) : IRequest<Result<DataRetentionResult>>;

/// <summary>
/// Result of the data retention cleanup operation.
/// </summary>
public sealed record DataRetentionResult(
    int UsersScanned,
    int WarningsSent,
    int DeletionsExecuted,
    int DeletionRequestsProcessed);
