using GuidedMentor.Mentoring.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.LocalDev.Mocks;

/// <summary>
/// No-op EventBridge publisher for local development.
/// Logs events to console instead of publishing to AWS EventBridge.
/// </summary>
public sealed class MockEventBridgePublisher : IEventBridgePublisher
{
    private readonly ILogger<MockEventBridgePublisher> _logger;

    public MockEventBridgePublisher(ILogger<MockEventBridgePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishSessionAcceptedAsync(
        Guid sessionId, Guid menteeId, Guid mentorId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV] EventBridge: SessionAccepted — session={SessionId}, mentee={MenteeId}, mentor={MentorId}",
            sessionId, menteeId, mentorId);
        return Task.CompletedTask;
    }

    public Task PublishSessionCompletedAsync(
        Guid sessionId, Guid menteeId, Guid mentorId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV] EventBridge: SessionCompleted — session={SessionId}, mentee={MenteeId}, mentor={MentorId}",
            sessionId, menteeId, mentorId);
        return Task.CompletedTask;
    }

    public Task ScheduleCompletionReminderAsync(
        Guid sessionId, Guid recipientId, DateTime fireAt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV] EventBridge: ScheduleReminder — session={SessionId}, recipient={RecipientId}, fireAt={FireAt}",
            sessionId, recipientId, fireAt);
        return Task.CompletedTask;
    }

    public Task ScheduleEscalationAsync(
        Guid sessionId, DateTime fireAt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV] EventBridge: ScheduleEscalation — session={SessionId}, fireAt={FireAt}",
            sessionId, fireAt);
        return Task.CompletedTask;
    }
}
