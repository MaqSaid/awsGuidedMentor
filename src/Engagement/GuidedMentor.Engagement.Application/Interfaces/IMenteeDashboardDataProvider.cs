using GuidedMentor.Engagement.Application.DTOs;

namespace GuidedMentor.Engagement.Application.Interfaces;

/// <summary>
/// Anti-corruption layer port for retrieving mentee dashboard data from cross-context sources
/// (Sessions_Table, Mentors_Table, and compatibility score service).
/// Implemented in the Infrastructure layer with DynamoDB queries.
/// </summary>
public interface IMenteeDashboardDataProvider
{
    /// <summary>
    /// Gets active session cards for the mentee (sessions in Active or MenteeCompleted status).
    /// Includes mentor name, session title, next incomplete follow-up task, and progress %.
    /// </summary>
    Task<IReadOnlyList<ActiveSessionCardDto>> GetActiveSessionsAsync(
        Guid menteeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top 3 recommended mentors ranked by compatibility score.
    /// Excludes mentors who are unavailable or at capacity.
    /// </summary>
    Task<IReadOnlyList<RecommendedMentorDto>> GetRecommendedMentorsAsync(
        Guid menteeId,
        int limit = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets progress statistics: completed sessions count, total/completed checklist items, overall %.
    /// </summary>
    Task<MenteeProgressStatsDto> GetProgressStatsAsync(
        Guid menteeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets summary bar counts: completed sessions, in-progress sessions, pending requests.
    /// </summary>
    Task<MenteeSummaryBarDto> GetSummaryBarAsync(
        Guid menteeId,
        CancellationToken cancellationToken = default);
}
