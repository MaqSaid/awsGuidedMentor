namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Service interface for data retention operations.
/// Implementation queries DynamoDB (Users_Table, Mentors_Table, Mentees_Table, Sessions_Table),
/// S3 (resume storage), Aurora (analytics), and Cognito for full account deletion.
///
/// Requirements: 21.16 (ISO 27001 — data retention policy)
/// </summary>
public interface IDataRetentionService
{
    /// <summary>
    /// Finds all user accounts where lastActivityAt is older than the specified cutoff date.
    /// </summary>
    Task<IReadOnlyList<InactiveUserRecord>> GetInactiveUsersSinceAsync(
        DateTime cutoffDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a 30-day retention warning notification to the user and records the warning timestamp.
    /// </summary>
    Task SendRetentionWarningAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves pending user-initiated deletion requests that have not yet been processed.
    /// </summary>
    Task<IReadOnlyList<DeletionRequest>> GetPendingDeletionRequestsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes full account deletion across all data stores:
    /// - Users_Table record
    /// - Mentors_Table record (if mentor profile exists)
    /// - Mentees_Table record (if mentee profile exists)
    /// - Sessions_Table records
    /// - S3 resume file (if uploaded)
    /// - Aurora analytics records (anonymised)
    /// - Cognito user pool record
    /// All actions are audit-logged.
    /// </summary>
    Task ExecuteFullDeletionAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an inactive user record identified during retention scanning.
/// </summary>
public sealed record InactiveUserRecord(
    Guid UserId,
    DateTime LastActivityAt,
    DateTime? RetentionWarningsentAt);

/// <summary>
/// Represents a pending user-initiated deletion request.
/// </summary>
public sealed record DeletionRequest(
    Guid UserId,
    DateTime RequestedAt);
