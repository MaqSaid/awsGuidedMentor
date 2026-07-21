using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.SharedInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that escalates sessions stuck in mentee_completed status.
/// Runs daily. Sessions in mentee_completed for 14+ days are escalated to unresolved
/// and both parties are notified.
/// </summary>
public sealed class SessionEscalationJob
{
    private readonly GuidedMentorDbContext _db;
    private readonly IMentoringNotificationPublisher _notificationPublisher;
    private readonly ILogger<SessionEscalationJob> _logger;

    public SessionEscalationJob(
        GuidedMentorDbContext db,
        IMentoringNotificationPublisher notificationPublisher,
        ILogger<SessionEscalationJob> logger)
    {
        _db = db;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting session escalation job");

        var cutoff = DateTime.UtcNow.AddDays(-14);
        var sessionsToEscalate = await _db.Sessions
            .Where(s => s.Status == "mentee_completed"
                     && s.MenteeCompletedAt != null
                     && s.MenteeCompletedAt < cutoff)
            .ToListAsync();

        if (sessionsToEscalate.Count == 0)
        {
            _logger.LogDebug("No sessions to escalate");
            return;
        }

        var escalatedCount = 0;
        foreach (var session in sessionsToEscalate)
        {
            session.Status = "unresolved";
            session.UpdatedAt = DateTime.UtcNow;
            escalatedCount++;

            await _notificationPublisher.NotifyEscalationAsync(
                session.MentorId,
                session.MenteeId,
                session.Id);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Escalated {Count} sessions to unresolved", escalatedCount);
    }
}
