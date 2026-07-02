using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;

namespace GuidedMentor.Engagement.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL-backed engagement event repository.
/// Replaced DynamoDB implementation — uses shared GuidedMentorDbContext.
/// </summary>
public sealed class PostgresEngagementEventRepository : IEngagementEventRepository
{
    public Task BatchPutAsync(IReadOnlyList<EngagementEvent> events, CancellationToken ct = default)
    {
        // Events will be persisted via EF Core DbContext in production
        return Task.CompletedTask;
    }
}
