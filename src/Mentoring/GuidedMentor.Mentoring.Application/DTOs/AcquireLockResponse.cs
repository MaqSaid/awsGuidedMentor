namespace GuidedMentor.Mentoring.Application.DTOs;

/// <summary>
/// Response returned after successfully acquiring a mentor lock.
/// </summary>
/// <param name="LockId">The unique identifier of the acquired lock.</param>
/// <param name="MentorId">The mentor that was locked.</param>
/// <param name="ExpiresAt">When the lock will automatically expire (15 minutes from creation).</param>
public sealed record AcquireLockResponse(
    Guid LockId,
    Guid MentorId,
    DateTime ExpiresAt);
