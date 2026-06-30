using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Represents a 15-minute hold a mentee places on a mentor during the selection process.
/// Uses DynamoDB conditional writes to prevent race conditions and TTL for automatic expiration.
/// </summary>
public sealed class MentorLock : Entity<LockId>
{
    /// <summary>The mentee who holds the lock.</summary>
    public MenteeId MenteeId { get; private set; }

    /// <summary>The mentor being locked.</summary>
    public MentorId MentorId { get; private set; }

    /// <summary>When the lock was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>When the lock expires (15 minutes from creation).</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Whether the lock has expired based on current UTC time.</summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    private MentorLock()
    {
        MenteeId = null!;
        MentorId = null!;
    }

    /// <summary>
    /// Creates a new MentorLock with a 15-minute TTL.
    /// </summary>
    /// <param name="menteeId">The mentee acquiring the lock.</param>
    /// <param name="mentorId">The mentor being locked.</param>
    /// <returns>A new MentorLock instance.</returns>
    public static MentorLock Create(MenteeId menteeId, MentorId mentorId)
    {
        var now = DateTime.UtcNow;
        return new MentorLock
        {
            Id = LockId.New(),
            MenteeId = menteeId,
            MentorId = mentorId,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(15)
        };
    }

    /// <summary>
    /// Reconstitutes a MentorLock from persisted data.
    /// </summary>
    public static MentorLock Reconstitute(
        LockId lockId,
        MenteeId menteeId,
        MentorId mentorId,
        DateTime createdAt,
        DateTime expiresAt)
    {
        return new MentorLock
        {
            Id = lockId,
            MenteeId = menteeId,
            MentorId = mentorId,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        };
    }
}
