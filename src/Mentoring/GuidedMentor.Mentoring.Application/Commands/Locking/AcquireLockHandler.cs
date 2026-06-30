using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Locking;

/// <summary>
/// Handles acquiring a mentor lock.
/// Validates the mentee doesn't already hold an active lock,
/// then performs a conditional write to DynamoDB.
/// </summary>
public sealed class AcquireLockHandler : IRequestHandler<AcquireLockCommand, Result<AcquireLockResponse>>
{
    private readonly ILockRepository _lockRepository;

    public AcquireLockHandler(ILockRepository lockRepository)
    {
        _lockRepository = lockRepository;
    }

    public async Task<Result<AcquireLockResponse>> Handle(
        AcquireLockCommand request,
        CancellationToken cancellationToken)
    {
        var menteeId = new MenteeId(request.MenteeId);
        var mentorId = new MentorId(request.MentorId);

        // Check if the mentee already holds an active lock on another mentor
        var existingLock = await _lockRepository.GetActiveLockForMenteeAsync(menteeId, cancellationToken);

        if (existingLock is not null && !existingLock.IsExpired)
        {
            return Result<AcquireLockResponse>.Failure(
                "You already have an active lock on a mentor. Release or confirm your current selection first.");
        }

        // Create the lock entity with 15-minute TTL
        var mentorLock = MentorLock.Create(menteeId, mentorId);

        // Attempt conditional write — prevents race conditions
        var acquired = await _lockRepository.TryAcquireLockAsync(mentorLock, cancellationToken);

        if (!acquired)
        {
            return Result<AcquireLockResponse>.Failure(
                "This mentor is temporarily unavailable. Another mentee is currently reviewing this mentor.");
        }

        return Result<AcquireLockResponse>.Success(
            new AcquireLockResponse(
                mentorLock.Id.Value,
                mentorLock.MentorId.Value,
                mentorLock.ExpiresAt));
    }
}
