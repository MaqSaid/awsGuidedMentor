using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.SharedInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that sends completion reminders to mentors.
/// Runs daily. Sessions in mentee_completed for 7+ days (but less than 14 days)
/// trigger a reminder to the mentor to confirm completion.
/// </summary>
public sealed class CompletionReminderJob
{
    private readonly GuidedMentorDbContext _db;
    private readonly IMentoringNotificationPublisher _notificationPublisher;
    private readonly ILogger<CompletionReminderJob> _logger;

    public CompletionReminderJob(
        GuidedMentorDbContext db,
        IMentoringNotificationPublisher notificationPublisher,
        ILogger<CompletionReminderJob> logger)
    {
        _db = db;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting completion reminder job");

        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var fourteenDaysAgo = now.AddDays(-14);

        var sessionsNeedingReminder = await _db.Sessions
            .Where(s => s.Status == "mentee_completed"
                     && s.MenteeCompletedAt != null
                     && s.MenteeCompletedAt < sevenDaysAgo
                     && s.MenteeCompletedAt >= fourteenDaysAgo)
            .Select(s => new { s.Id, s.MentorId })
            .ToListAsync();

        if (sessionsNeedingReminder.Count == 0)
        {
            _logger.LogDebug("No sessions need completion reminders");
            return;
        }

        var reminderCount = 0;
        foreach (var session in sessionsNeedingReminder)
        {
            await _notificationPublisher.SendCompletionReminderAsync(
                session.MentorId,
                session.Id);
            reminderCount++;
        }

        _logger.LogInformation("Sent {Count} completion reminders to mentors", reminderCount);
    }
}
