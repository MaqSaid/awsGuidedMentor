using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Handles declining a mentorship session request.
/// Notifies the mentee, removes the session record, and releases the mentor slot.
/// </summary>
public sealed class DeclineRequestHandler : IRequestHandler<DeclineRequestCommand, Result>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMentoringNotificationPublisher _notificationPublisher;

    public DeclineRequestHandler(
        ISessionRepository sessionRepository,
        IMentoringNotificationPublisher notificationPublisher)
    {
        _sessionRepository = sessionRepository;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result> Handle(DeclineRequestCommand request, CancellationToken cancellationToken)
    {
        var sessionId = new SessionId(request.SessionId);

        // Retrieve the session
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure("Session not found.");
        }

        // Verify the request is for the correct mentor
        if (session.MentorId.Value != request.MentorId)
        {
            return Result.Failure("You are not the mentor for this session.");
        }

        // Apply domain logic — validates the session is in PendingAcceptance status
        var declineResult = session.Decline();
        if (declineResult.IsFailure)
        {
            return declineResult;
        }

        // Remove the session record from the dashboard
        await _sessionRepository.DeleteAsync(sessionId, cancellationToken);

        // Notify the mentee of the decline
        await _notificationPublisher.NotifyMenteeOfDeclineAsync(
            session.MenteeId.Value,
            request.MentorId,
            request.SessionId,
            cancellationToken);

        return Result.Success();
    }
}
