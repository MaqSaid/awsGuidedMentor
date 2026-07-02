namespace GuidedMentor.Engagement.Infrastructure.Persistence;

/// <summary>
/// Placeholder — DynamoDB Stream replication is no longer needed since
/// we now use PostgreSQL as the single source of truth for both transactional
/// and analytics data. This file is kept for backward compatibility.
/// </summary>
public sealed class StreamReplicationHandler
{
    // No-op: With PostgreSQL as the single database, there's no need for
    // DynamoDB Stream → Aurora replication. Analytics queries run directly
    // against the same PostgreSQL database.
}
