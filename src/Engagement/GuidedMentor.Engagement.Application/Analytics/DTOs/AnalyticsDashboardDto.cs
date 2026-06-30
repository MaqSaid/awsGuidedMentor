namespace GuidedMentor.Engagement.Application.Analytics.DTOs;

/// <summary>
/// Response DTO for the operator analytics dashboard.
/// Contains DAU/WAU/MAU, feature heatmap, error hotspots, and retention metrics.
///
/// Requirements: 30.4, 30.6
/// </summary>
public sealed record AnalyticsDashboardDto(
    ActiveUsersMetrics ActiveUsers,
    IReadOnlyList<FeatureUsageDto> FeatureHeatmap,
    IReadOnlyList<ErrorHotspotDto> ErrorHotspots,
    RetentionMetrics Retention);

/// <summary>
/// Daily, Weekly, and Monthly active user counts.
/// </summary>
public sealed record ActiveUsersMetrics(
    int DailyActiveUsers,
    int WeeklyActiveUsers,
    int MonthlyActiveUsers);

/// <summary>
/// Feature usage frequency for the heatmap display.
/// </summary>
public sealed record FeatureUsageDto(
    string FeatureName,
    int UsageCount,
    int UniqueUsers);

/// <summary>
/// Error hotspot showing pages with highest error rates.
/// </summary>
public sealed record ErrorHotspotDto(
    string PageContext,
    int ErrorCount,
    IReadOnlyList<string> TopErrorTypes);

/// <summary>
/// Retention metrics (7-day and 30-day return rates).
/// </summary>
public sealed record RetentionMetrics(
    double SevenDayRetentionRate,
    double ThirtyDayRetentionRate);
