using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.AuditLogging;

/// <summary>
/// Writes audit log records using Serilog structured logging to a dedicated CloudWatch log group ("audit-log").
/// Uses a dedicated ILogger category so that Serilog routing can direct these entries
/// to the appropriate CloudWatch log group via log group configuration.
/// </summary>
public sealed class CloudWatchAuditLogWriter : IAuditLogWriter
{
    private readonly ILogger<CloudWatchAuditLogWriter> _logger;

    public CloudWatchAuditLogWriter(ILogger<CloudWatchAuditLogWriter> logger)
    {
        _logger = logger;
    }

    public Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
    {
        if (record.AdminId is not null)
        {
            _logger.LogInformation(
                "AUDIT: {Action} by Admin:{AdminId} on {Resource} targeting {AdminTarget} reason:{AdminReason} " +
                "user:{UserId} correlationId:{CorrelationId} success:{Success} at {Timestamp}",
                record.Action,
                record.AdminId,
                record.Resource,
                record.AdminTarget,
                record.AdminReason,
                record.UserId,
                record.CorrelationId,
                record.Success,
                record.Timestamp);
        }
        else
        {
            _logger.LogInformation(
                "AUDIT: {Action} by {UserId} on {Resource} correlationId:{CorrelationId} success:{Success} at {Timestamp}",
                record.Action,
                record.UserId,
                record.Resource,
                record.CorrelationId,
                record.Success,
                record.Timestamp);
        }

        return Task.CompletedTask;
    }
}
