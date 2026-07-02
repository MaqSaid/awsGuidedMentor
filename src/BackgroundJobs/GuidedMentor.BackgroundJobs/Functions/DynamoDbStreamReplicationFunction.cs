namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Placeholder — DynamoDB Stream replication function is no longer needed.
/// With PostgreSQL as the single source of truth, there's no DynamoDB → Aurora sync.
/// This file is kept to avoid breaking the project structure.
/// Background jobs will be migrated to Hangfire in a future step.
/// </summary>
public sealed class StreamReplicationPlaceholder
{
    // No-op: This Lambda function has been retired.
    // Analytics queries now run directly against the same PostgreSQL database.
}
