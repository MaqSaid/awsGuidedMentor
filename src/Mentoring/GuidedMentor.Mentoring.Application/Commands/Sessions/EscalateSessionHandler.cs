using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Handles session escalation after 14 days without mentor confirmation.
/// Transitions session to Unresolved and notifies both parties.
/// </summary>
public sealed class EscalateSessionHandler : IRequestHandler<EscalateSessionCommand, Result>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMentoringNotificationPublisher _notificationPublisher;

    public EscalateSessionHandler(
        ISessionRepository sessionRepository,
        IMentoringNotificationPublisher notificationPublisher)
    {
        _sessionRepository = sessionRepository;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result> Handle(EscalateSessionCommand request, CancellationToken cancellationToken)
    {
        var sessionId = new SessionId(request.SessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure("Session not found.");
        }

        // Only escalate if still in MenteeCompleted status
        // (If mentor confirmed between scheduling and firing, this is a no-op)
        if (session.Status != SessionStatus.MenteeCompleted)
        {
            return Result.Success();
        }

        var escalateResult = session.Escalate();
        if (escalateResult.IsFailure)
        {
            return escalateResult;
        }

        await _sessionRepository.SaveAsync(session, cancellationToken);

        // Notify both parties about the escalation
        await _notificationPublisher.NotifyEscalationAsync(
            session.MentorId.Value,
            session.MenteeId.Value,
            session.Id.Value,
            cancellationToken);

        return Result.Success();
    }
}
