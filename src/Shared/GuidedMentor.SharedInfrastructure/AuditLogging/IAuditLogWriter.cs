namespace GuidedMentor.SharedInfrastructure.AuditLogging;

/// <summary>
/// Writes audit log records to the dedicated audit CloudWatch log group.
/// </summary>
public interface IAuditLogWriter
{
    /// <summary>
    /// Writes an audit log record. Implementation should use Serilog structured logging
    /// directed to the "audit-log" CloudWatch log group.
    /// </summary>
    /// <param name="record">The audit record to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default);
}
