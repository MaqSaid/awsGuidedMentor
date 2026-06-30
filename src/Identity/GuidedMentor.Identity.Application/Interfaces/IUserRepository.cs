namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Repository interface for user persistence operations.
/// Implementation lives in the Infrastructure layer.
/// </summary>
public interface IUserRepository
{
    Task<UserLockoutInfo?> GetLockoutInfoAsync(string email, CancellationToken ct);
    Task IncrementFailedAttemptsAsync(string email, CancellationToken ct);
    Task ResetFailedAttemptsAsync(string email, CancellationToken ct);
    Task LockAccountAsync(string email, DateTime lockedUntil, CancellationToken ct);
    Task<string?> GetUserIdByEmailAsync(string email, CancellationToken ct);
}

/// <summary>
/// Represents lockout state for a user.
/// </summary>
public sealed record UserLockoutInfo(
    int FailedLoginAttempts,
    DateTime? LockedUntil,
    DateTime? FirstFailedAttemptAt);
