using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GuidedMentor.Engagement.Infrastructure.Persistence;

/// <summary>
/// Lambda handler for replicating DynamoDB Stream events to Aurora PostgreSQL.
/// Processes INSERT, MODIFY, and REMOVE events from DynamoDB Streams and
/// upserts/deletes corresponding rows in the analytics schema.
///
/// Supports replication from:
/// - Users table → analytics.users
/// - Sessions table → analytics.sessions
/// - Matches (compatibility scores) → analytics.matches
/// - EngagementEvents table → analytics.engagement_metrics
///
/// Requirements: 30.9, 16.7
/// </summary>
public sealed class DynamoDbStreamReplicationHandler
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DynamoDbStreamReplicationHandler> _logger;

    public DynamoDbStreamReplicationHandler(
        NpgsqlDataSource dataSource,
        ILogger<DynamoDbStreamReplicationHandler> logger)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Process a batch of DynamoDB Stream records and replicate to Aurora.
    /// </summary>
    public async Task HandleAsync(DynamoDbStreamEvent streamEvent, CancellationToken ct = default)
    {
        if (streamEvent.Records is null || streamEvent.Records.Count == 0)
        {
            _logger.LogDebug("No records to process in stream event");
            return;
        }

        _logger.LogInformation("Processing {Count} DynamoDB Stream records", streamEvent.Records.Count);

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            foreach (var record in streamEvent.Records)
            {
                await ProcessRecordAsync(record, connection, transaction, ct);
            }

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Successfully replicated {Count} records to Aurora",
                streamEvent.Records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replicate stream records to Aurora, rolling back");
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task ProcessRecordAsync(
        DynamoDbStreamRecord record,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        var sourceTable = record.EventSourceArn?.Split('/').ElementAtOrDefault(1) ?? string.Empty;

        switch (sourceTable)
        {
            case var t when t.Contains("Users", StringComparison.OrdinalIgnoreCase):
                await ReplicateUserAsync(record, connection, transaction, ct);
                break;

            case var t when t.Contains("Sessions", StringComparison.OrdinalIgnoreCase):
                await ReplicateSessionAsync(record, connection, transaction, ct);
                break;

            case var t when t.Contains("Mentors", StringComparison.OrdinalIgnoreCase)
                         || t.Contains("Mentees", StringComparison.OrdinalIgnoreCase):
                await ReplicateMatchAsync(record, connection, transaction, ct);
                break;

            case var t when t.Contains("EngagementEvents", StringComparison.OrdinalIgnoreCase):
                await ReplicateEngagementMetricAsync(record, connection, transaction, ct);
                break;

            default:
                _logger.LogWarning("Unknown source table: {Table}", sourceTable);
                break;
        }
    }

    private async Task ReplicateUserAsync(
        DynamoDbStreamRecord record,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        if (record.EventName == "REMOVE")
        {
            var userId = GetStringAttribute(record.DynamoDb?.OldImage, "userId");
            if (string.IsNullOrEmpty(userId)) return;

            const string deleteSql = "DELETE FROM analytics.users WHERE user_id = @userId::UUID";
            await using var cmd = new NpgsqlCommand(deleteSql, connection, transaction);
            cmd.Parameters.AddWithValue("userId", Guid.Parse(userId));
            await cmd.ExecuteNonQueryAsync(ct);
            return;
        }

        var image = record.DynamoDb?.NewImage;
        if (image is null) return;

        const string upsertSql = """
            INSERT INTO analytics.users (user_id, email, active_role, aws_chapter, city,
                mentor_onboarding_completed, mentee_onboarding_completed, created_at)
            VALUES (@userId::UUID, @email, @activeRole, @chapter, @city,
                @mentorOnboarded, @menteeOnboarded, @createdAt)
            ON CONFLICT (user_id) DO UPDATE SET
                email = EXCLUDED.email,
                active_role = EXCLUDED.active_role,
                aws_chapter = EXCLUDED.aws_chapter,
                city = EXCLUDED.city,
                mentor_onboarding_completed = EXCLUDED.mentor_onboarding_completed,
                mentee_onboarding_completed = EXCLUDED.mentee_onboarding_completed
            """;

        await using var cmd2 = new NpgsqlCommand(upsertSql, connection, transaction);
        cmd2.Parameters.AddWithValue("userId", Guid.Parse(GetStringAttribute(image, "userId") ?? Guid.Empty.ToString()));
        cmd2.Parameters.AddWithValue("email", GetStringAttribute(image, "email") ?? string.Empty);
        cmd2.Parameters.AddWithValue("activeRole", (object?)GetStringAttribute(image, "activeRole") ?? DBNull.Value);
        cmd2.Parameters.AddWithValue("chapter", (object?)GetStringAttribute(image, "awsChapter") ?? DBNull.Value);
        cmd2.Parameters.AddWithValue("city", (object?)GetStringAttribute(image, "city") ?? DBNull.Value);
        cmd2.Parameters.AddWithValue("mentorOnboarded", GetBoolAttribute(image, "mentorOnboardingCompleted"));
        cmd2.Parameters.AddWithValue("menteeOnboarded", GetBoolAttribute(image, "menteeOnboardingCompleted"));
        cmd2.Parameters.AddWithValue("createdAt", GetTimestampAttribute(image, "createdAt"));

        await cmd2.ExecuteNonQueryAsync(ct);
    }

    private async Task ReplicateSessionAsync(
        DynamoDbStreamRecord record,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        if (record.EventName == "REMOVE")
        {
            var sessionId = GetStringAttribute(record.DynamoDb?.OldImage, "sessionId");
            if (string.IsNullOrEmpty(sessionId)) return;

            const string deleteSql = "DELETE FROM analytics.sessions WHERE session_id = @sessionId::UUID";
            await using var cmd = new NpgsqlCommand(deleteSql, connection, transaction);
            cmd.Parameters.AddWithValue("sessionId", Guid.Parse(sessionId));
            await cmd.ExecuteNonQueryAsync(ct);
            return;
        }

        var image = record.DynamoDb?.NewImage;
        if (image is null) return;

        const string upsertSql = """
            INSERT INTO analytics.sessions (session_id, mentee_id, mentor_id, status,
                plan_generated_at, mentee_completed_at, mentor_completed_at,
                plan_retry_count, checklist_total, checklist_completed, created_at, updated_at)
            VALUES (@sessionId::UUID, @menteeId::UUID, @mentorId::UUID, @status,
                @planGeneratedAt, @menteeCompletedAt, @mentorCompletedAt,
                @planRetryCount, @checklistTotal, @checklistCompleted, @createdAt, @updatedAt)
            ON CONFLICT (session_id) DO UPDATE SET
                status = EXCLUDED.status,
                plan_generated_at = EXCLUDED.plan_generated_at,
                mentee_completed_at = EXCLUDED.mentee_completed_at,
                mentor_completed_at = EXCLUDED.mentor_completed_at,
                plan_retry_count = EXCLUDED.plan_retry_count,
                checklist_total = EXCLUDED.checklist_total,
                checklist_completed = EXCLUDED.checklist_completed,
                updated_at = EXCLUDED.updated_at
            """;

        await using var cmd2 = new NpgsqlCommand(upsertSql, connection, transaction);
        cmd2.Parameters.AddWithValue("sessionId", Guid.Parse(GetStringAttribute(image, "sessionId") ?? Guid.Empty.ToString()));
        cmd2.Parameters.AddWithValue("menteeId", Guid.Parse(GetStringAttribute(image, "menteeId") ?? Guid.Empty.ToString()));
        cmd2.Parameters.AddWithValue("mentorId", Guid.Parse(GetStringAttribute(image, "mentorId") ?? Guid.Empty.ToString()));
        cmd2.Parameters.AddWithValue("status", GetStringAttribute(image, "status") ?? "unknown");
        cmd2.Parameters.AddWithValue("planGeneratedAt", (object?)GetNullableTimestamp(image, "planGeneratedAt") ?? DBNull.Value);
        cmd2.Parameters.AddWithValue("menteeCompletedAt", (object?)GetNullableTimestamp(image, "menteeCompletedAt") ?? DBNull.Value);
        cmd2.Parameters.AddWithValue("mentorCompletedAt", (object?)GetNullableTimestamp(image, "mentorCompletedAt") ?? DBNull.Value);
        cmd2.Parameters.AddWithValue("planRetryCount", GetIntAttribute(image, "planRetryCount"));
        cmd2.Parameters.AddWithValue("checklistTotal", GetIntAttribute(image, "checklistTotal"));
        cmd2.Parameters.AddWithValue("checklistCompleted", GetIntAttribute(image, "checklistCompleted"));
        cmd2.Parameters.AddWithValue("createdAt", GetTimestampAttribute(image, "createdAt"));
        cmd2.Parameters.AddWithValue("updatedAt", GetTimestampAttribute(image, "updatedAt"));

        await cmd2.ExecuteNonQueryAsync(ct);
    }

    private async Task ReplicateMatchAsync(
        DynamoDbStreamRecord record,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        // Matches are derived from compatibility score events in the Mentors/Mentees tables
        var image = record.DynamoDb?.NewImage;
        if (image is null || record.EventName == "REMOVE") return;

        // Only process if this record contains a compatibility score (match event)
        var compatibilityScore = GetStringAttribute(image, "compatibilityScore");
        if (string.IsNullOrEmpty(compatibilityScore)) return;

        const string upsertSql = """
            INSERT INTO analytics.matches (match_id, mentee_id, mentor_id, compatibility_score,
                chapter_score, skills_overlap, goal_alignment, experience_gap,
                mentee_chapter, mentor_chapter, created_at)
            VALUES (@matchId::UUID, @menteeId::UUID, @mentorId::UUID, @score,
                @chapterScore, @skillsOverlap, @goalAlignment, @experienceGap,
                @menteeChapter, @mentorChapter, @createdAt)
            ON CONFLICT (match_id) DO UPDATE SET
                compatibility_score = EXCLUDED.compatibility_score,
                chapter_score = EXCLUDED.chapter_score,
                skills_overlap = EXCLUDED.skills_overlap,
                goal_alignment = EXCLUDED.goal_alignment,
                experience_gap = EXCLUDED.experience_gap
            """;

        await using var cmd = new NpgsqlCommand(upsertSql, connection, transaction);
        cmd.Parameters.AddWithValue("matchId", Guid.Parse(GetStringAttribute(image, "matchId") ?? Guid.NewGuid().ToString()));
        cmd.Parameters.AddWithValue("menteeId", Guid.Parse(GetStringAttribute(image, "menteeId") ?? Guid.Empty.ToString()));
        cmd.Parameters.AddWithValue("mentorId", Guid.Parse(GetStringAttribute(image, "mentorId") ?? Guid.Empty.ToString()));
        cmd.Parameters.AddWithValue("score", int.Parse(compatibilityScore));
        cmd.Parameters.AddWithValue("chapterScore", GetIntAttribute(image, "chapterScore"));
        cmd.Parameters.AddWithValue("skillsOverlap", GetIntAttribute(image, "skillsOverlap"));
        cmd.Parameters.AddWithValue("goalAlignment", GetIntAttribute(image, "goalAlignment"));
        cmd.Parameters.AddWithValue("experienceGap", GetIntAttribute(image, "experienceGap"));
        cmd.Parameters.AddWithValue("menteeChapter", GetStringAttribute(image, "menteeChapter") ?? "unknown");
        cmd.Parameters.AddWithValue("mentorChapter", GetStringAttribute(image, "mentorChapter") ?? "unknown");
        cmd.Parameters.AddWithValue("createdAt", GetTimestampAttribute(image, "createdAt"));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task ReplicateEngagementMetricAsync(
        DynamoDbStreamRecord record,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        if (record.EventName == "REMOVE") return; // TTL deletions — no need to replicate

        var image = record.DynamoDb?.NewImage;
        if (image is null) return;

        const string upsertSql = """
            INSERT INTO analytics.engagement_metrics (metric_id, user_id, event_type, metadata, occurred_at)
            VALUES (@metricId::UUID, @userId::UUID, @eventType, @metadata::JSONB, @occurredAt)
            ON CONFLICT (metric_id) DO NOTHING
            """;

        var eventDataJson = GetStringAttribute(image, "eventData");
        var metadataJson = string.IsNullOrEmpty(eventDataJson) ? "{}" : eventDataJson;

        // Include pageContext and activeRole in the metadata for analytics queries
        var pageContext = GetStringAttribute(image, "pageContext") ?? string.Empty;
        var activeRole = GetStringAttribute(image, "activeRole") ?? string.Empty;

        // Merge additional context into metadata
        try
        {
            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson)
                           ?? new Dictionary<string, object>();
            metadata["pageContext"] = pageContext;
            metadata["activeRole"] = activeRole;
            metadataJson = JsonSerializer.Serialize(metadata);
        }
        catch
        {
            // If deserialization fails, wrap in object
            metadataJson = JsonSerializer.Serialize(new { pageContext, activeRole, raw = metadataJson });
        }

        var userIdHash = GetStringAttribute(image, "userIdHash") ?? Guid.Empty.ToString();
        // Use the eventId from DynamoDB as the metric_id
        var eventId = GetStringAttribute(image, "eventId") ?? Guid.NewGuid().ToString();

        // The user_id in Aurora uses the hashed userId; generate a deterministic UUID from hash
        var userId = GenerateUuidFromHash(userIdHash);

        var timestamp = GetLongAttribute(image, "timestamp");
        var occurredAt = timestamp > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime
            : DateTime.UtcNow;

        await using var cmd = new NpgsqlCommand(upsertSql, connection, transaction);
        cmd.Parameters.AddWithValue("metricId", Guid.TryParse(eventId, out var eid) ? eid : Guid.NewGuid());
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("eventType", GetStringAttribute(image, "eventType") ?? "unknown");
        cmd.Parameters.AddWithValue("metadata", metadataJson);
        cmd.Parameters.AddWithValue("occurredAt", occurredAt);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Generate a deterministic UUID v5 from a SHA-256 hash string.
    /// </summary>
    private static Guid GenerateUuidFromHash(string hash)
    {
        if (Guid.TryParse(hash, out var guid))
            return guid;

        // Take first 16 bytes of the hash hex string to form a UUID
        var hexBytes = hash.Length >= 32 ? hash[..32] : hash.PadRight(32, '0');
        var bytes = Convert.FromHexString(hexBytes);
        return new Guid(bytes);
    }

    private static string? GetStringAttribute(Dictionary<string, StreamAttributeValue>? image, string key)
    {
        if (image is null || !image.TryGetValue(key, out var attr))
            return null;
        return attr.S;
    }

    private static int GetIntAttribute(Dictionary<string, StreamAttributeValue>? image, string key)
    {
        if (image is null || !image.TryGetValue(key, out var attr))
            return 0;
        return int.TryParse(attr.N, out var val) ? val : 0;
    }

    private static long GetLongAttribute(Dictionary<string, StreamAttributeValue>? image, string key)
    {
        if (image is null || !image.TryGetValue(key, out var attr))
            return 0;
        return long.TryParse(attr.N, out var val) ? val : 0;
    }

    private static bool GetBoolAttribute(Dictionary<string, StreamAttributeValue>? image, string key)
    {
        if (image is null || !image.TryGetValue(key, out var attr))
            return false;
        return attr.BOOL;
    }

    private static DateTime GetTimestampAttribute(Dictionary<string, StreamAttributeValue>? image, string key)
    {
        var value = GetStringAttribute(image, key);
        if (string.IsNullOrEmpty(value))
            return DateTime.UtcNow;
        return DateTime.TryParse(value, out var dt) ? dt.ToUniversalTime() : DateTime.UtcNow;
    }

    private static DateTime? GetNullableTimestamp(Dictionary<string, StreamAttributeValue>? image, string key)
    {
        var value = GetStringAttribute(image, key);
        if (string.IsNullOrEmpty(value))
            return null;
        return DateTime.TryParse(value, out var dt) ? dt.ToUniversalTime() : null;
    }
}

/// <summary>
/// Represents a DynamoDB Stream event containing multiple records.
/// </summary>
public sealed class DynamoDbStreamEvent
{
    public List<DynamoDbStreamRecord>? Records { get; set; }
}

/// <summary>
/// Represents a single DynamoDB Stream record.
/// </summary>
public sealed class DynamoDbStreamRecord
{
    public string? EventName { get; set; }
    public string? EventSourceArn { get; set; }
    public StreamRecordData? DynamoDb { get; set; }
}

/// <summary>
/// Contains the DynamoDB stream record data (new and old image).
/// </summary>
public sealed class StreamRecordData
{
    public Dictionary<string, StreamAttributeValue>? NewImage { get; set; }
    public Dictionary<string, StreamAttributeValue>? OldImage { get; set; }
}

/// <summary>
/// Simplified DynamoDB attribute value for stream processing.
/// Named StreamAttributeValue to avoid conflict with Amazon.DynamoDBv2.Model.AttributeValue.
/// </summary>
public sealed class StreamAttributeValue
{
    public string? S { get; set; }
    public string? N { get; set; }
    public bool BOOL { get; set; }
    public string? M { get; set; }
}
