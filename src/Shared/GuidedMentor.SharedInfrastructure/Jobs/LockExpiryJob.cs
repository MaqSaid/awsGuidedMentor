using GuidedMentor.SharedInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that releases expired mentor locks from sessions.
/// Runs every 5 minutes, clearing LockId/LockExpiresAt on sessions
/// where the lock has been held longer than 15 minutes.
/// </summary>
public sealed class LockExpiryJob
{
    private readonly GuidedMentorDbContext _db;
    private readonly ILogger<LockExpiryJob> _logger;

    public LockExpiryJob(GuidedMentorDbContext db, ILogger<LockExpiryJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting lock expiry cleanup job");

        var now = DateTime.UtcNow;
        var released = await _db.Sessions
            .Where(s => s.LockId != null && s.LockExpiresAt != null && s.LockExpiresAt < now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.LockId, (Guid?)null)
                .SetProperty(s => s.LockExpiresAt, (DateTime?)null)
                .SetProperty(s => s.UpdatedAt, now));

        _logger.LogInformation("Released {Count} expired mentor locks", released);
    }
}
