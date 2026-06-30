namespace GuidedMentor.SharedKernel;

/// <summary>
/// Marker interface for MediatR commands that should be audit-logged.
/// When a request implements this interface, the AuditLoggingBehavior will
/// record who, when, what, which resource, and correlationId to the audit log.
/// </summary>
public interface IAuditableCommand
{
    /// <summary>
    /// The ID of the user performing the action (extracted from JWT).
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// The resource identifier affected by this command (e.g., "Mentor:abc-123").
    /// </summary>
    string AuditResourceId { get; }
}
