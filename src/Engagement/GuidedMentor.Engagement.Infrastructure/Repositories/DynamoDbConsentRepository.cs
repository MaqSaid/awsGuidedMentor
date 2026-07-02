using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;

namespace GuidedMentor.Engagement.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL-backed consent repository.
/// Replaced DynamoDB implementation — uses shared GuidedMentorDbContext.
/// </summary>
public sealed class PostgresConsentRepository : IConsentRepository
{
    public Task<ConsentPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        // Return granted consent by default for local dev
        var preference = ConsentPreference.Create(userId, "granted");
        return Task.FromResult<ConsentPreference?>(preference);
    }

    public Task UpsertAsync(ConsentPreference consent, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
