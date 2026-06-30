namespace GuidedMentor.Engagement.Application.DTOs;

/// <summary>
/// Rich DTO for the mentee dashboard, aggregated from multiple data sources.
/// Consumed directly by the frontend — no additional transformation needed.
/// </summary>
public sealed record MenteeDashboardDto(
    IReadOnlyList<ActiveSessionCardDto> ActiveSessions,
    IReadOnlyList<RecommendedMentorDto> RecommendedMentors,
    MenteeProgressStatsDto Stats,
    MenteeSummaryBarDto SummaryBar,
    DashboardSectionErrorDto? SessionsError,
    DashboardSectionErrorDto? RecommendationsError,
    DashboardSectionErrorDto? StatsError);

/// <summary>
/// A card representing an active session on the mentee dashboard.
/// </summary>
public sealed record ActiveSessionCardDto(
    Guid SessionId,
    string MentorName,
    string SessionTitle,
    string? NextFollowUpTask,
    int ProgressPercentage);

/// <summary>
/// A recommended mentor card with compatibility score.
/// </summary>
public sealed record RecommendedMentorDto(
    Guid MentorId,
    string MentorName,
    string? Chapter,
    int CompatibilityScore,
    IReadOnlyList<string> ExpertiseAreas);

/// <summary>
/// Progress statistics for the mentee: completed sessions, checklist progress.
/// </summary>
public sealed record MenteeProgressStatsDto(
    int CompletedSessionsCount,
    int TotalChecklistItems,
    int CompletedChecklistItems,
    int OverallCompletionPercentage);

/// <summary>
/// Summary bar counts for the mentee dashboard.
/// </summary>
public sealed record MenteeSummaryBarDto(
    int CompletedSessions,
    int InProgressSessions,
    int PendingRequests);

/// <summary>
/// Per-section error with retry information; preserves other loaded sections.
/// </summary>
public sealed record DashboardSectionErrorDto(
    string Section,
    string Message,
    bool CanRetry);
