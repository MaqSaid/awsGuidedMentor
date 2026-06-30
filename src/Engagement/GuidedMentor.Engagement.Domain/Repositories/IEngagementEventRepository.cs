using GuidedMentor.Engagement.Domain.Entities;

namespace GuidedMentor.Engagement.Domain.Repositories;

/// <summary>
/// Repository interface for persisting engagement events to EngagementEvents_Table.
/// </summary>
public interface IEngagementEventRepository
{
    /// <summary>
    /// Batch persist a collection of engagement events.
    /// </summary>
    Task BatchPutAsync(IReadOnlyList<EngagementEvent> events, CancellationToken ct = default);
}
