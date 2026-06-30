namespace GuidedMentor.Engagement.Application.Analytics.DTOs;

/// <summary>
/// Response DTO for engagement-specific analytics metrics.
/// Tracks browse-to-lock conversion, plan-to-completion rate, and job view-to-click rate.
///
/// Requirements: 30.5
/// </summary>
public sealed record EngagementAnalyticsDto(
    double BrowseToLockConversionRate,
    double PlanToCompletionRate,
    double JobViewToClickRate,
    IReadOnlyList<EngagementMetricBreakdownDto> DailyBreakdown);

/// <summary>
/// Daily breakdown of engagement metrics.
/// </summary>
public sealed record EngagementMetricBreakdownDto(
    DateOnly Date,
    int BrowseEvents,
    int LockEvents,
    int PlanViews,
    int Completions,
    int JobViews,
    int JobClicks);
