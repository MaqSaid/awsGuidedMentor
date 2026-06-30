using GuidedMentor.Mentoring.Domain.Entities;

namespace GuidedMentor.Mentoring.Domain.Repositories;

/// <summary>
/// Repository interface for opportunity posting data access.
/// Backed by the DynamoDB Opportunities table (formerly Jobs table).
/// </summary>
public interface IOpportunityRepository
{
    /// <summary>
    /// Gets an opportunity posting by its ID.
    /// </summary>
    Task<OpportunityPosting?> GetByIdAsync(OpportunityPostingId id, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of active postings for a mentor (all types combined).
    /// Used to enforce the max 5 active postings limit.
    /// </summary>
    Task<int> GetActiveCountByMentorAsync(MentorId mentorId, CancellationToken ct = default);

    /// <summary>
    /// Gets all postings for a specific mentor (all statuses).
    /// Uses GSI-Mentor (PK=mentorId).
    /// </summary>
    Task<IReadOnlyList<OpportunityPosting>> GetByMentorAsync(MentorId mentorId, CancellationToken ct = default);

    /// <summary>
    /// Saves (creates or updates) an opportunity posting.
    /// </summary>
    Task SaveAsync(OpportunityPosting posting, CancellationToken ct = default);

    /// <summary>
    /// Queries active postings with optional filters, sorted by publishedAt descending.
    /// Uses GSI-Status (PK=status, SK=expiresAt) for efficient querying.
    /// </summary>
    Task<(IReadOnlyList<OpportunityPosting> Items, int TotalCount)> BrowseAsync(
        OpportunityType? typeFilter,
        string? locationFilter,
        IReadOnlyList<string>? skillsFilter,
        ExperienceLevel? experienceFilter,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active postings that have expired (ExpiresAt &lt; UtcNow) or past-event.
    /// Used by the daily expiry job.
    /// </summary>
    Task<IReadOnlyList<OpportunityPosting>> GetExpiredActivePostingsAsync(CancellationToken ct = default);
}
