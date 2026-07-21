using GuidedMentor.Engagement.Application.Analytics.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;

namespace GuidedMentor.LocalDev.Mocks;

/// <summary>
/// No-op analytics repository for local development.
/// Returns realistic sample data without requiring Aurora PostgreSQL.
/// </summary>
public sealed class MockAnalyticsRepository : IAnalyticsRepository
{
    public Task<ActiveUsersMetrics> GetActiveUsersAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new ActiveUsersMetrics(
            DailyActiveUsers: 42,
            WeeklyActiveUsers: 185,
            MonthlyActiveUsers: 520));
    }

    public Task<IReadOnlyList<FeatureUsageDto>> GetFeatureHeatmapAsync(
        int topN = 20,
        CancellationToken ct = default)
    {
        IReadOnlyList<FeatureUsageDto> data = new List<FeatureUsageDto>
        {
            new("Browse Mentors", 320, 95),
            new("Session Plan", 180, 65),
            new("AI Assistant", 150, 58),
            new("Dashboard", 420, 110),
            new("Notifications", 95, 72)
        };
        return Task.FromResult(data);
    }

    public Task<IReadOnlyList<ErrorHotspotDto>> GetErrorHotspotsAsync(
        int topN = 10,
        CancellationToken ct = default)
    {
        IReadOnlyList<ErrorHotspotDto> data = new List<ErrorHotspotDto>
        {
            new("/onboarding", 3, new List<string> { "ValidationError" }),
            new("/sessions/plan", 1, new List<string> { "TimeoutError" })
        };
        return Task.FromResult(data);
    }

    public Task<RetentionMetrics> GetRetentionMetricsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new RetentionMetrics(
            SevenDayRetentionRate: 68.5,
            ThirtyDayRetentionRate: 45.2));
    }

    public Task<FunnelDataDto> GetFunnelDataAsync(CancellationToken ct = default)
    {
        IReadOnlyList<FunnelStageDto> stages = new List<FunnelStageDto>
        {
            new("Signup", 520, 100.0, 0.0),
            new("Onboard", 410, 78.8, 21.2),
            new("Browse", 350, 85.4, 14.6),
            new("Match", 180, 51.4, 48.6),
            new("Session", 120, 66.7, 33.3),
            new("Complete", 85, 70.8, 29.2)
        };
        return Task.FromResult(new FunnelDataDto(stages, 16.3));
    }

    public Task<EngagementAnalyticsDto> GetEngagementAnalyticsAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default)
    {
        IReadOnlyList<EngagementMetricBreakdownDto> breakdown = new List<EngagementMetricBreakdownDto>
        {
            new(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), 45, 8, 30, 5, 20, 6)
        };
        return Task.FromResult(new EngagementAnalyticsDto(
            BrowseToLockConversionRate: 17.8,
            PlanToCompletionRate: 70.8,
            JobViewToClickRate: 30.0,
            DailyBreakdown: breakdown));
    }
}
