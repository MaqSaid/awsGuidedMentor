using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Locking;

/// <summary>
/// Handles confirming a mentor selection.
/// Validates the lock exists, is not expired, and belongs to the mentee.
/// Creates a pending session, notifies the mentor, and consumes the lock.
/// </summary>
public sealed class ConfirmSelectionHandler : IRequestHandler<ConfirmSelectionCommand, Result<ConfirmSelectionResponse>>
{
    private readonly ILockRepository _lockRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IMentoringNotificationPublisher _notificationPublisher;

    public ConfirmSelectionHandler(
        ILockRepository lockRepository,
        ISessionRepository sessionRepository,
        IMentoringNotificationPublisher notificationPublisher)
    {
        _lockRepository = lockRepository;
        _sessionRepository = sessionRepository;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<ConfirmSelectionResponse>> Handle(
        ConfirmSelectionCommand request,
        CancellationToken cancellationToken)
    {
        var lockId = new LockId(request.LockId);
        var menteeId = new MenteeId(request.MenteeId);
        var mentorId = new MentorId(request.MentorId);

        // Retrieve and validate the lock
        var existingLock = await _lockRepository.GetByIdAsync(lockId, cancellationToken);

        if (existingLock is null)
        {
            return Result<ConfirmSelectionResponse>.Failure("Lock not found. It may have expired.");
        }

        // Verify the lock belongs to the requesting mentee
        if (existingLock.MenteeId != menteeId)
        {
            return Result<ConfirmSelectionResponse>.Failure("This lock does not belong to you.");
        }

        // Verify the lock is for the correct mentor
        if (existingLock.MentorId != mentorId)
        {
            return Result<ConfirmSelectionResponse>.Failure("Lock mentor mismatch.");
        }

        // Check expiration
        if (existingLock.IsExpired)
        {
            return Result<ConfirmSelectionResponse>.Failure(
                "Lock has expired. Please acquire a new lock on this mentor.");
        }

        // Create the pending session
        var sessionId = SessionId.New();

        await _sessionRepository.CreatePendingSessionAsync(
            sessionId,
            menteeId,
            mentorId,
            lockId,
            cancellationToken);

        // Notify the mentor of the pending request
        await _notificationPublisher.NotifyMentorOfSelectionAsync(
            request.MentorId,
            request.MenteeId,
            sessionId.Value,
            cancellationToken);

        // Consume the lock (release it after session is created)
        await _lockRepository.ReleaseLockAsync(lockId, cancellationToken);

        return Result<ConfirmSelectionResponse>.Success(
            new ConfirmSelectionResponse(
                sessionId.Value,
                request.MentorId,
                "PendingAcceptance"));
    }
}
