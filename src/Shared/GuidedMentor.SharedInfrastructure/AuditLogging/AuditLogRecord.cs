namespace GuidedMentor.SharedInfrastructure.AuditLogging;

/// <summary>
/// Represents a single audit log record written to the dedicated CloudWatch log group.
/// Records who, when, what, which resource, and correlationId for every state change.
/// </summary>
public sealed record AuditLogRecord
{
    /// <summary>
    /// The user who performed the action (from JWT sub claim).
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// UTC timestamp of when the action occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// The type of action performed (command type name, e.g., "UpdateSettingsCommand").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// The resource affected by this action (e.g., "Mentor:abc-123", "User:def-456").
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// Correlation ID linking this audit entry to the request trace.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Whether the command completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Optional admin ID for Super Admin operations.
    /// </summary>
    public string? AdminId { get; init; }

    /// <summary>
    /// Optional target for Super Admin operations (e.g., target user ID).
    /// </summary>
    public string? AdminTarget { get; init; }

    /// <summary>
    /// Optional reason for Super Admin operations.
    /// </summary>
    public string? AdminReason { get; init; }
}
