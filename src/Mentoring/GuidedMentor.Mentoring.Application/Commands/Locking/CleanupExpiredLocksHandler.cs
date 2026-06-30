using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Mentoring.Application.Commands.Locking;

/// <summary>
/// Handles cleanup of expired mentor locks.
/// Queries for expired locks and removes them from DynamoDB.
/// While DynamoDB TTL handles eventual cleanup (up to 48h delay),
/// this provides near-real-time lock release for browse availability.
/// </summary>
public sealed class CleanupExpiredLocksHandler : IRequestHandler<CleanupExpiredLocksCommand, Result>
{
    private readonly ILockRepository _lockRepository;
    private readonly ILogger<CleanupExpiredLocksHandler> _logger;

    public CleanupExpiredLocksHandler(
        ILockRepository lockRepository,
        ILogger<CleanupExpiredLocksHandler> logger)
    {
        _lockRepository = lockRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(CleanupExpiredLocksCommand request, CancellationToken cancellationToken)
    {
        var expiredLocks = await _lockRepository.GetExpiredLocksAsync(cancellationToken);

        if (expiredLocks.Count == 0)
        {
            _logger.LogDebug("No expired locks found during cleanup.");
            return Result.Success();
        }

        _logger.LogInformation("Found {Count} expired locks to clean up.", expiredLocks.Count);

        var cleanedCount = 0;
        foreach (var lockEntity in expiredLocks)
        {
            try
            {
                await _lockRepository.ReleaseLockAsync(lockEntity.Id, cancellationToken);
                cleanedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to release expired lock {LockId} for mentor {MentorId}.",
                    lockEntity.Id.Value,
                    lockEntity.MentorId.Value);
            }
        }

        _logger.LogInformation("Cleaned up {CleanedCount}/{TotalCount} expired locks.",
            cleanedCount, expiredLocks.Count);

        return Result.Success();
    }
}
