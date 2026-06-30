using GuidedMentor.Mentoring.Domain.Entities;

namespace GuidedMentor.Mentoring.Domain.Repositories;

/// <summary>
/// Repository interface for MentorLock persistence.
/// Implementation uses DynamoDB conditional writes for atomic lock acquisition
/// and TTL for automatic lock expiration cleanup.
/// </summary>
public interface ILockRepository
{
    /// <summary>
    /// Attempts to acquire a lock using a DynamoDB conditional write.
    /// The conditional expression ensures the lock is only created if no active lock
    /// exists for the mentor (attribute_not_exists OR lockExpiresAt &lt; now).
    /// </summary>
    /// <param name="mentorLock">The lock to acquire.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the lock was acquired; false if another mentee holds an active lock on this mentor.</returns>
    Task<bool> TryAcquireLockAsync(MentorLock mentorLock, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases (deletes) an existing lock, making the mentor available to other mentees.
    /// </summary>
    /// <param name="lockId">The lock to release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ReleaseLockAsync(LockId lockId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active (non-expired) lock held by a specific mentee, if any.
    /// </summary>
    /// <param name="menteeId">The mentee to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active lock, or null if the mentee has no active lock.</returns>
    Task<MentorLock?> GetActiveLockForMenteeAsync(MenteeId menteeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a lock by its identifier.
    /// </summary>
    /// <param name="lockId">The lock identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lock, or null if not found.</returns>
    Task<MentorLock?> GetByIdAsync(LockId lockId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all locks that have expired (ExpiresAt &lt; UtcNow).
    /// Used by the 5-minute EventBridge Scheduler cleanup job.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All expired locks that have not yet been cleaned up by DynamoDB TTL.</returns>
    Task<IReadOnlyList<MentorLock>> GetExpiredLocksAsync(CancellationToken cancellationToken = default);
}
