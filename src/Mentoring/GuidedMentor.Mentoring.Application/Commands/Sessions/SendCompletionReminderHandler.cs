using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Handles sending a 7-day completion reminder.
/// Verifies the session is still awaiting mentor confirmation before sending.
/// </summary>
public sealed class SendCompletionReminderHandler : IRequestHandler<SendCompletionReminderCommand, Result>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMentoringNotificationPublisher _notificationPublisher;

    public SendCompletionReminderHandler(
        ISessionRepository sessionRepository,
        IMentoringNotificationPublisher notificationPublisher)
    {
        _sessionRepository = sessionRepository;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result> Handle(SendCompletionReminderCommand request, CancellationToken cancellationToken)
    {
        var sessionId = new SessionId(request.SessionId);

        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure("Session not found.");
        }

        // Only send reminder if still in MenteeCompleted status
        // (If mentor already confirmed, this is a no-op)
        if (session.Status != SessionStatus.MenteeCompleted)
        {
            return Result.Success();
        }

        await _notificationPublisher.SendCompletionReminderAsync(
            request.RecipientId,
            request.SessionId,
            cancellationToken);

        return Result.Success();
    }
}
