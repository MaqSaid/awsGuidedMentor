using GuidedMentor.Engagement.Domain.Entities;

namespace GuidedMentor.Engagement.Domain.Repositories;

/// <summary>
/// Repository interface for persisting user consent preferences.
/// </summary>
public interface IConsentRepository
{
    Task<ConsentPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(ConsentPreference consent, CancellationToken ct = default);
}
