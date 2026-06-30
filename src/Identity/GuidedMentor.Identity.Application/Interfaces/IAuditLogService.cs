namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Records admin actions for audit trail purposes.
/// Every Super_Admin action (disable user, toggle feature, maintenance mode)
/// is recorded with adminId, timestamp, action, target, and reason.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Records an admin action in the audit log.
    /// </summary>
    Task RecordAsync(AuditLogEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the most recent audit log entries (default last 50).
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetRecentAsync(int limit = 50, CancellationToken ct = default);
}

/// <summary>
/// Represents a single audit log entry.
/// </summary>
public sealed record AuditLogEntry(
    Guid AdminId,
    DateTime Timestamp,
    string Action,
    string TargetResource,
    string Reason);
