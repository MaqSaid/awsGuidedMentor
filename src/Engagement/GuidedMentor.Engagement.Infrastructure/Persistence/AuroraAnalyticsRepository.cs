using System.Data;
using GuidedMentor.Engagement.Application.Analytics.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GuidedMentor.Engagement.Infrastructure.Persistence;

/// <summary>
/// Aurora PostgreSQL implementation of the analytics repository.
/// Queries denormalised reporting data replicated from DynamoDB via Streams + Lambda.
/// Connected through RDS Proxy for Lambda connection pooling.
///
/// Requirements: 30.4, 30.5, 30.6, 30.9
/// </summary>
public sealed class AuroraAnalyticsRepository : IAnalyticsRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<AuroraAnalyticsRepository> _logger;

    public AuroraAnalyticsRepository(
        NpgsqlDataSource dataSource,
        ILogger<AuroraAnalyticsRepository> logger)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ActiveUsersMetrics> GetActiveUsersAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COUNT(DISTINCT user_id) FILTER (
                    WHERE occurred_at >= NOW() - INTERVAL '1 day'
                ) AS dau,
                COUNT(DISTINCT user_id) FILTER (
                    WHERE occurred_at >= NOW() - INTERVAL '7 days'
                ) AS wau,
                COUNT(DISTINCT user_id) FILTER (
                    WHERE occurred_at >= NOW() - INTERVAL '30 days'
                ) AS mau
            FROM analytics.engagement_metrics
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            return new ActiveUsersMetrics(
                DailyActiveUsers: reader.GetInt32(0),
                WeeklyActiveUsers: reader.GetInt32(1),
                MonthlyActiveUsers: reader.GetInt32(2));
        }

        return new ActiveUsersMetrics(0, 0, 0);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FeatureUsageDto>> GetFeatureHeatmapAsync(
        int topN = 20,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                event_type AS feature_name,
                COUNT(*) AS usage_count,
                COUNT(DISTINCT user_id) AS unique_users
            FROM analytics.engagement_metrics
            WHERE occurred_at >= NOW() - INTERVAL '30 days'
              AND event_type NOT IN ('error_encountered')
            GROUP BY event_type
            ORDER BY usage_count DESC
            LIMIT @topN
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("topN", topN);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var results = new List<FeatureUsageDto>();
        while (await reader.ReadAsync(ct))
        {
            results.Add(new FeatureUsageDto(
                FeatureName: reader.GetString(0),
                UsageCount: reader.GetInt32(1),
                UniqueUsers: reader.GetInt32(2)));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ErrorHotspotDto>> GetErrorHotspotsAsync(
        int topN = 10,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COALESCE(metadata->>'pageContext', 'unknown') AS page_context,
                COUNT(*) AS error_count,
                ARRAY_AGG(DISTINCT COALESCE(metadata->>'errorType', 'unknown'))
                    AS top_error_types
            FROM analytics.engagement_metrics
            WHERE event_type = 'error_encountered'
              AND occurred_at >= NOW() - INTERVAL '30 days'
            GROUP BY page_context
            ORDER BY error_count DESC
            LIMIT @topN
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("topN", topN);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var results = new List<ErrorHotspotDto>();
        while (await reader.ReadAsync(ct))
        {
            var errorTypes = reader.GetFieldValue<string[]>(2);
            results.Add(new ErrorHotspotDto(
                PageContext: reader.GetString(0),
                ErrorCount: reader.GetInt32(1),
                TopErrorTypes: errorTypes.Take(5).ToList()));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<RetentionMetrics> GetRetentionMetricsAsync(CancellationToken ct = default)
    {
        // 7-day retention: users who were active 7+ days ago AND also active in last 7 days
        // 30-day retention: users who were active 30+ days ago AND also active in last 30 days
        const string sql = """
            WITH cohort_7d AS (
                SELECT DISTINCT user_id
                FROM analytics.engagement_metrics
                WHERE occurred_at BETWEEN NOW() - INTERVAL '14 days' AND NOW() - INTERVAL '7 days'
            ),
            returned_7d AS (
                SELECT DISTINCT em.user_id
                FROM analytics.engagement_metrics em
                INNER JOIN cohort_7d c ON em.user_id = c.user_id
                WHERE em.occurred_at >= NOW() - INTERVAL '7 days'
            ),
            cohort_30d AS (
                SELECT DISTINCT user_id
                FROM analytics.engagement_metrics
                WHERE occurred_at BETWEEN NOW() - INTERVAL '60 days' AND NOW() - INTERVAL '30 days'
            ),
            returned_30d AS (
                SELECT DISTINCT em.user_id
                FROM analytics.engagement_metrics em
                INNER JOIN cohort_30d c ON em.user_id = c.user_id
                WHERE em.occurred_at >= NOW() - INTERVAL '30 days'
            )
            SELECT
                COALESCE(
                    ROUND(
                        (SELECT COUNT(*)::NUMERIC FROM returned_7d) /
                        NULLIF((SELECT COUNT(*)::NUMERIC FROM cohort_7d), 0) * 100, 1
                    ), 0
                ) AS seven_day_retention,
                COALESCE(
                    ROUND(
                        (SELECT COUNT(*)::NUMERIC FROM returned_30d) /
                        NULLIF((SELECT COUNT(*)::NUMERIC FROM cohort_30d), 0) * 100, 1
                    ), 0
                ) AS thirty_day_retention
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            return new RetentionMetrics(
                SevenDayRetentionRate: reader.GetDouble(0),
                ThirtyDayRetentionRate: reader.GetDouble(1));
        }

        return new RetentionMetrics(0, 0);
    }

    /// <inheritdoc />
    public async Task<FunnelDataDto> GetFunnelDataAsync(CancellationToken ct = default)
    {
        // Count users at each funnel stage based on their engagement events and user state
        const string sql = """
            WITH signup_users AS (
                SELECT COUNT(DISTINCT user_id) AS cnt FROM analytics.users
            ),
            onboarded_users AS (
                SELECT COUNT(DISTINCT user_id) AS cnt FROM analytics.users
                WHERE mentor_onboarding_completed = TRUE OR mentee_onboarding_completed = TRUE
            ),
            browse_users AS (
                SELECT COUNT(DISTINCT user_id) AS cnt
                FROM analytics.engagement_metrics
                WHERE event_type = 'page_view'
                  AND metadata->>'pageName' ILIKE '%browse%'
            ),
            match_users AS (
                SELECT COUNT(DISTINCT mentee_id) AS cnt FROM analytics.matches
            ),
            session_users AS (
                SELECT COUNT(DISTINCT mentee_id) AS cnt FROM analytics.sessions
            ),
            completed_users AS (
                SELECT COUNT(DISTINCT mentee_id) AS cnt FROM analytics.sessions
                WHERE status = 'completed'
            )
            SELECT
                (SELECT cnt FROM signup_users) AS signup_count,
                (SELECT cnt FROM onboarded_users) AS onboard_count,
                (SELECT cnt FROM browse_users) AS browse_count,
                (SELECT cnt FROM match_users) AS match_count,
                (SELECT cnt FROM session_users) AS session_count,
                (SELECT cnt FROM completed_users) AS complete_count
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            var signupCount = reader.GetInt32(0);
            var onboardCount = reader.GetInt32(1);
            var browseCount = reader.GetInt32(2);
            var matchCount = reader.GetInt32(3);
            var sessionCount = reader.GetInt32(4);
            var completeCount = reader.GetInt32(5);

            var stages = BuildFunnelStages(
                signupCount, onboardCount, browseCount,
                matchCount, sessionCount, completeCount);

            var overallRate = signupCount > 0
                ? Math.Round((double)completeCount / signupCount * 100, 1)
                : 0;

            return new FunnelDataDto(Stages: stages, OverallConversionRate: overallRate);
        }

        return new FunnelDataDto(Stages: [], OverallConversionRate: 0);
    }

    /// <inheritdoc />
    public async Task<EngagementAnalyticsDto> GetEngagementAnalyticsAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Aggregate conversion metrics
        const string rateSql = """
            SELECT
                COUNT(*) FILTER (WHERE event_type = 'page_view' AND metadata->>'pageName' ILIKE '%browse%') AS browse_events,
                COUNT(*) FILTER (WHERE event_type = 'click' AND metadata->>'element' = 'lock_mentor') AS lock_events,
                COUNT(*) FILTER (WHERE event_type = 'page_view' AND metadata->>'pageName' ILIKE '%session_plan%') AS plan_views,
                COUNT(*) FILTER (WHERE event_type = 'click' AND metadata->>'element' = 'complete_checklist') AS completions,
                COUNT(*) FILTER (WHERE event_type = 'page_view' AND metadata->>'pageName' ILIKE '%opportunit%') AS job_views,
                COUNT(*) FILTER (WHERE event_type = 'job_click') AS job_clicks
            FROM analytics.engagement_metrics
            WHERE occurred_at >= @fromDate::DATE AND occurred_at < @toDate::DATE + INTERVAL '1 day'
            """;

        // Daily breakdown
        const string breakdownSql = """
            SELECT
                occurred_at::DATE AS event_date,
                COUNT(*) FILTER (WHERE event_type = 'page_view' AND metadata->>'pageName' ILIKE '%browse%') AS browse_events,
                COUNT(*) FILTER (WHERE event_type = 'click' AND metadata->>'element' = 'lock_mentor') AS lock_events,
                COUNT(*) FILTER (WHERE event_type = 'page_view' AND metadata->>'pageName' ILIKE '%session_plan%') AS plan_views,
                COUNT(*) FILTER (WHERE event_type = 'click' AND metadata->>'element' = 'complete_checklist') AS completions,
                COUNT(*) FILTER (WHERE event_type = 'page_view' AND metadata->>'pageName' ILIKE '%opportunit%') AS job_views,
                COUNT(*) FILTER (WHERE event_type = 'job_click') AS job_clicks
            FROM analytics.engagement_metrics
            WHERE occurred_at >= @fromDate::DATE AND occurred_at < @toDate::DATE + INTERVAL '1 day'
            GROUP BY event_date
            ORDER BY event_date
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);

        // Aggregate rates
        await using var rateCmd = new NpgsqlCommand(rateSql, connection);
        rateCmd.Parameters.AddWithValue("fromDate", fromDate.ToDateTime(TimeOnly.MinValue));
        rateCmd.Parameters.AddWithValue("toDate", toDate.ToDateTime(TimeOnly.MinValue));
        await using var rateReader = await rateCmd.ExecuteReaderAsync(ct);

        int browseEvents = 0, lockEvents = 0, planViews = 0, completions = 0, jobViews = 0, jobClicks = 0;
        if (await rateReader.ReadAsync(ct))
        {
            browseEvents = rateReader.GetInt32(0);
            lockEvents = rateReader.GetInt32(1);
            planViews = rateReader.GetInt32(2);
            completions = rateReader.GetInt32(3);
            jobViews = rateReader.GetInt32(4);
            jobClicks = rateReader.GetInt32(5);
        }

        await rateReader.CloseAsync();

        // Daily breakdown
        await using var breakdownCmd = new NpgsqlCommand(breakdownSql, connection);
        breakdownCmd.Parameters.AddWithValue("fromDate", fromDate.ToDateTime(TimeOnly.MinValue));
        breakdownCmd.Parameters.AddWithValue("toDate", toDate.ToDateTime(TimeOnly.MinValue));
        await using var breakdownReader = await breakdownCmd.ExecuteReaderAsync(ct);

        var dailyBreakdown = new List<EngagementMetricBreakdownDto>();
        while (await breakdownReader.ReadAsync(ct))
        {
            dailyBreakdown.Add(new EngagementMetricBreakdownDto(
                Date: DateOnly.FromDateTime(breakdownReader.GetDateTime(0)),
                BrowseEvents: breakdownReader.GetInt32(1),
                LockEvents: breakdownReader.GetInt32(2),
                PlanViews: breakdownReader.GetInt32(3),
                Completions: breakdownReader.GetInt32(4),
                JobViews: breakdownReader.GetInt32(5),
                JobClicks: breakdownReader.GetInt32(6)));
        }

        var browseToLock = browseEvents > 0
            ? Math.Round((double)lockEvents / browseEvents * 100, 1)
            : 0;
        var planToCompletion = planViews > 0
            ? Math.Round((double)completions / planViews * 100, 1)
            : 0;
        var jobViewToClick = jobViews > 0
            ? Math.Round((double)jobClicks / jobViews * 100, 1)
            : 0;

        return new EngagementAnalyticsDto(
            BrowseToLockConversionRate: browseToLock,
            PlanToCompletionRate: planToCompletion,
            JobViewToClickRate: jobViewToClick,
            DailyBreakdown: dailyBreakdown);
    }

    private static IReadOnlyList<FunnelStageDto> BuildFunnelStages(
        int signup, int onboard, int browse, int match, int session, int complete)
    {
        var stages = new List<FunnelStageDto>
        {
            BuildStage("Signup", signup, signup),
            BuildStage("Onboard", onboard, signup),
            BuildStage("Browse", browse, onboard),
            BuildStage("Match", match, browse),
            BuildStage("Session", session, match),
            BuildStage("Complete", complete, session),
        };

        return stages;
    }

    private static FunnelStageDto BuildStage(string name, int count, int previousCount)
    {
        var conversionRate = previousCount > 0
            ? Math.Round((double)count / previousCount * 100, 1)
            : 0;
        var dropOffRate = previousCount > 0
            ? Math.Round((1.0 - (double)count / previousCount) * 100, 1)
            : 0;

        return new FunnelStageDto(
            StageName: name,
            UserCount: count,
            ConversionRateFromPrevious: conversionRate,
            DropOffRate: Math.Max(0, dropOffRate));
    }
}
