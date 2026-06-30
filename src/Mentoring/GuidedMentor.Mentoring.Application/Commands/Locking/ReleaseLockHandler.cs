using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Locking;

/// <summary>
/// Handles releasing a mentor lock.
/// Validates the lock belongs to the requesting mentee before deleting.
/// </summary>
public sealed class ReleaseLockHandler : IRequestHandler<ReleaseLockCommand, Result>
{
    private readonly ILockRepository _lockRepository;

    public ReleaseLockHandler(ILockRepository lockRepository)
    {
        _lockRepository = lockRepository;
    }

    public async Task<Result> Handle(
        ReleaseLockCommand request,
        CancellationToken cancellationToken)
    {
        var lockId = new LockId(request.LockId);
        var menteeId = new MenteeId(request.MenteeId);

        var existingLock = await _lockRepository.GetByIdAsync(lockId, cancellationToken);

        if (existingLock is null)
        {
            return Result.Failure("Lock not found.");
        }

        // Verify the lock belongs to the requesting mentee
        if (existingLock.MenteeId != menteeId)
        {
            return Result.Failure("You can only release your own locks.");
        }

        await _lockRepository.ReleaseLockAsync(lockId, cancellationToken);

        return Result.Success();
    }
}
