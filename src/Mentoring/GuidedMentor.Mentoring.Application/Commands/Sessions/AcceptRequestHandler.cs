using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Sessions;

/// <summary>
/// Handles accepting a mentorship session request.
/// Validates the mentor is below capacity, updates session status to PendingPlan (triggering plan generation),
/// increments mentor activeMenteeCount, and publishes session-accepted event.
/// </summary>
public sealed class AcceptRequestHandler : IRequestHandler<AcceptRequestCommand, Result>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IMentorRepository _mentorRepository;
    private readonly IMentoringNotificationPublisher _notificationPublisher;
    private readonly IEventBridgePublisher _eventBridgePublisher;

    public AcceptRequestHandler(
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

    public async Task<Result> Handle(AcceptRequestCommand request, CancellationToken cancellationToken)
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

        // Check mentor capacity
        var mentor = await _mentorRepository.GetByIdAsync(request.MentorId, cancellationToken);
        if (mentor is null)
        {
            return Result.Failure("Mentor profile not found.");
        }

        if (mentor.ActiveMenteeCount >= mentor.MaxMentees)
        {
            return Result.Failure("You have reached your maximum mentee capacity. Please complete or decline existing sessions before accepting new ones.");
        }

        // Apply domain logic — transitions status from PendingAcceptance to PendingPlan
        var acceptResult = session.Accept();
        if (acceptResult.IsFailure)
        {
            return acceptResult;
        }

        // Persist the session state change
        await _sessionRepository.SaveAsync(session, cancellationToken);

        // Increment mentor capacity
        await _mentorRepository.IncrementActiveMenteeCountAsync(request.MentorId, cancellationToken);

        // Notify the mentee that the request was accepted
        await _notificationPublisher.NotifyMenteeOfAcceptanceAsync(
            session.MenteeId.Value,
            request.MentorId,
            request.SessionId,
            cancellationToken);

        // Publish session-accepted event to trigger plan generation
        await _eventBridgePublisher.PublishSessionAcceptedAsync(
            request.SessionId,
            session.MenteeId.Value,
            request.MentorId,
            cancellationToken);

        return Result.Success();
    }
}
