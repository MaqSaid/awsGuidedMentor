using GuidedMentor.SharedInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that cleans up expired and used auth tokens from PostgreSQL.
/// Runs every 5 minutes, replacing DynamoDB TTL auto-expiry.
/// </summary>
public sealed class CleanupExpiredTokensJob
{
    private readonly GuidedMentorDbContext _db;
    private readonly ILogger<CleanupExpiredTokensJob> _logger;

    public CleanupExpiredTokensJob(GuidedMentorDbContext db, ILogger<CleanupExpiredTokensJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting expired token cleanup job");

        var cutoff = DateTime.UtcNow;
        var deleted = await _db.AuthTokens
            .Where(t => t.ExpiresAt < cutoff || t.Used)
            .ExecuteDeleteAsync();

        _logger.LogInformation("Cleaned up {Count} expired/used auth tokens", deleted);
    }
}
