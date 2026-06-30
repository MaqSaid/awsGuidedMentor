using GuidedMentor.Engagement.Application.Analytics.DTOs;

namespace GuidedMentor.Engagement.Application.Interfaces;

/// <summary>
/// Repository interface for querying analytics data from Aurora PostgreSQL.
/// Used by the operator analytics dashboard (admin only).
/// Data is replicated from DynamoDB via Streams → Lambda.
///
/// Requirements: 30.4, 30.5, 30.6, 30.9
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>
    /// Returns the count of distinct active users within the specified period.
    /// DAU = last 24h, WAU = last 7 days, MAU = last 30 days.
    /// </summary>
    Task<ActiveUsersMetrics> GetActiveUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns feature usage counts ranked by frequency (heatmap data).
    /// </summary>
    Task<IReadOnlyList<FeatureUsageDto>> GetFeatureHeatmapAsync(
        int topN = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Returns pages with highest error rates (error hotspots).
    /// </summary>
    Task<IReadOnlyList<ErrorHotspotDto>> GetErrorHotspotsAsync(
        int topN = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Returns 7-day and 30-day user retention rates.
    /// </summary>
    Task<RetentionMetrics> GetRetentionMetricsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the conversion funnel data:
    /// signup → onboard → browse → match → session → complete.
    /// </summary>
    Task<FunnelDataDto> GetFunnelDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns engagement-specific metrics:
    /// browse-to-lock conversion, plan-to-completion rate, job view-to-click rate.
    /// </summary>
    Task<EngagementAnalyticsDto> GetEngagementAnalyticsAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default);
}
