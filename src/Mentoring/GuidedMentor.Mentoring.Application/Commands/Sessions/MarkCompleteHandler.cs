using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Handles the two-party completion flow state machine.
/// When mentee marks complete: schedules 7-day reminder and 14-day escalation via EventBridge.
/// When mentor confirms: decrements activeMenteeCount and publishes session-completed event.
/// </summary>
public sealed class MarkCompleteHandler : IRequestHandler<MarkCompleteCommand, Result>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMentorRepository _mentorRepository;
    private readonly IMentoringNotificationPublisher _notificationPublisher;
    private readonly IEventBridgePublisher _eventBridgePublisher;

    public MarkCompleteHandler(
        ISessionRepository sessionRepository,
        IMentorRepository mentorRepository,
        IMentoringNotificationPublisher notificationPublisher,
        IEventBridgePublisher eventBridgePublisher)
    {
        _sessionRepository = sessionRepository;
        _mentorRepository = mentorRepository;
        _notificationPublisher = notificationPublisher;
        _eventBridgePublisher = eventBridgePublisher;
    }

    public async Task<Result> Handle(MarkCompleteCommand request, CancellationToken cancellationToken)
    {
        var sessionId = new SessionId(request.SessionId);

        // Retrieve the session
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure("Session not found.");
        }

        // Verify the user belongs to this session
        var isParticipant = request.Role == Role.Mentee
            ? session.MenteeId.Value == request.UserId
            : session.MentorId.Value == request.UserId;

        if (!isParticipant)
        {
            return Result.Failure("You are not a participant in this session.");
        }

        // Apply the completion state machine
        var completeResult = session.MarkComplete(request.Role);
        if (completeResult.IsFailure)
        {
            return completeResult;
        }

        // Persist the state change
        await _sessionRepository.SaveAsync(session, cancellationToken);

        // Handle post-transition side effects based on the new status
        if (session.Status == SessionStatus.MenteeCompleted)
        {
            await HandleMenteeCompletedAsync(session, cancellationToken);
        }
        else if (session.Status == SessionStatus.Completed)
        {
            await HandleFullyCompletedAsync(session, cancellationToken);
        }

        return Result.Success();
    }

    private async Task HandleMenteeCompletedAsync(Session session, CancellationToken cancellationToken)
    {
        // Notify the mentor to confirm completion
        await _notificationPublisher.NotifyMentorOfCompletionMarkAsync(
            session.MentorId.Value,
            session.MenteeId.Value,
            session.Id.Value,
            cancellationToken);

        // Schedule 7-day reminder for the mentor
        var reminderDate = session.MenteeCompletedAt!.Value.AddDays(7);
        await _eventBridgePublisher.ScheduleCompletionReminderAsync(
            session.Id.Value,
            session.MentorId.Value,
            reminderDate,
            cancellationToken);

        // Schedule 14-day escalation to unresolved
        var escalationDate = session.MenteeCompletedAt!.Value.AddDays(14);
        await _eventBridgePublisher.ScheduleEscalationAsync(
            session.Id.Value,
            escalationDate,
            cancellationToken);
    }

    private async Task HandleFullyCompletedAsync(Session session, CancellationToken cancellationToken)
    {
        // Decrement the mentor's active mentee count
        await _mentorRepository.DecrementActiveMenteeCountAsync(
            session.MentorId.Value,
            cancellationToken);

        // Notify both parties of completion
        await _notificationPublisher.NotifySessionCompletedAsync(
            session.MentorId.Value,
            session.MenteeId.Value,
            session.Id.Value,
            cancellationToken);

        // Publish session-completed event to EventBridge for analytics
        await _eventBridgePublisher.PublishSessionCompletedAsync(
            session.Id.Value,
            session.MenteeId.Value,
            session.MentorId.Value,
            cancellationToken);
    }
}
