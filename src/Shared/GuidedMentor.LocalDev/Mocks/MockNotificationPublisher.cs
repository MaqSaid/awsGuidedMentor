using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;

namespace GuidedMentor.LocalDev.Mocks;

/// <summary>
/// No-op notification publisher for local development.
/// Logs notifications to console instead of publishing to EventBridge.
/// </summary>
internal sealed class MockNotificationPublisher : IMentoringNotificationPublisher
{
    public Task NotifyMentorOfSelectionAsync(Guid mentorId, Guid menteeId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Mentor {mentorId} selected by mentee {menteeId} for session {sessionId}");
        return Task.CompletedTask;
    }

    public Task NotifyMenteeOfAcceptanceAsync(Guid menteeId, Guid mentorId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Mentee {menteeId} accepted by mentor {mentorId} for session {sessionId}");
        return Task.CompletedTask;
    }

    public Task NotifyMenteeOfDeclineAsync(Guid menteeId, Guid mentorId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Mentee {menteeId} declined by mentor {mentorId} for session {sessionId}");
        return Task.CompletedTask;
    }

    public Task NotifyMentorOfCompletionMarkAsync(Guid mentorId, Guid menteeId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Mentor {mentorId} — mentee {menteeId} marked session {sessionId} complete");
        return Task.CompletedTask;
    }

    public Task NotifySessionCompletedAsync(Guid mentorId, Guid menteeId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Session {sessionId} completed (mentor: {mentorId}, mentee: {menteeId})");
        return Task.CompletedTask;
    }

    public Task SendCompletionReminderAsync(Guid recipientId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Completion reminder sent to {recipientId} for session {sessionId}");
        return Task.CompletedTask;
    }

    public Task NotifyEscalationAsync(Guid mentorId, Guid menteeId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Session {sessionId} escalated (mentor: {mentorId}, mentee: {menteeId})");
        return Task.CompletedTask;
    }

    public Task NotifyMenteeOfNewOpportunityAsync(Guid menteeId, Guid postingId, Guid mentorId, OpportunityType type, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Mentee {menteeId} — new {type} opportunity {postingId} from mentor {mentorId}");
        return Task.CompletedTask;
    }

    public Task NotifyMenteeOfSkillMatchedOpportunityAsync(Guid menteeId, Guid postingId, Guid mentorId, OpportunityType type, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Mentee {menteeId} — skill-matched {type} opportunity {postingId} from mentor {mentorId}");
        return Task.CompletedTask;
    }

    public Task NotifyMentorOpportunityExpiredWithRenewalAsync(Guid mentorId, Guid postingId, string postingTitle, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Mentor {mentorId} — opportunity '{postingTitle}' expired (renewal available)");
        return Task.CompletedTask;
    }

    public Task NotifyMentorOpportunityExpiredAsync(Guid mentorId, Guid postingId, string postingTitle, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Mentor {mentorId} — opportunity '{postingTitle}' expired");
        return Task.CompletedTask;
    }

    public Task SendAvailabilityReminderAsync(Guid mentorId, string displayName, DateTime unavailableSince, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Notification] Availability reminder for mentor {mentorId} ({displayName}), unavailable since {unavailableSince:d}");
        return Task.CompletedTask;
    }
}
