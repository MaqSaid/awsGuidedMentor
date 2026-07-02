using GuidedMentor.SharedInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that archives expired opportunity postings.
/// Runs daily at midnight UTC.
/// </summary>
public sealed class OpportunityExpiryJob
{
    private readonly GuidedMentorDbContext _db;
    private readonly ILogger<OpportunityExpiryJob> _logger;

    public OpportunityExpiryJob(GuidedMentorDbContext db, ILogger<OpportunityExpiryJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting opportunity expiry job");

        var now = DateTime.UtcNow;
        var archived = await _db.Opportunities
            .Where(o => o.IsActive && o.ExpiresAt != null && o.ExpiresAt < now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.IsActive, false));

        _logger.LogInformation("Archived {Count} expired opportunities", archived);
    }
}
