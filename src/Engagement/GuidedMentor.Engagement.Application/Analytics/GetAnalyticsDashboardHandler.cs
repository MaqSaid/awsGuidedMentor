using GuidedMentor.Engagement.Application.Analytics.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Handles the operator analytics dashboard query.
/// Queries Aurora PostgreSQL via the analytics repository for:
/// - DAU/WAU/MAU (calculated from engagement events)
/// - Feature usage heatmap (most/least used features)
/// - Error hotspots (pages with highest error rates)
/// - Retention metrics (7-day, 30-day return rates)
///
/// Admin-only access enforced at the API layer.
///
/// Requirements: 30.4, 30.6
/// </summary>
public sealed class GetAnalyticsDashboardHandler : IRequestHandler<GetAnalyticsDashboardQuery, AnalyticsDashboardDto>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ILogger<GetAnalyticsDashboardHandler> _logger;

    public GetAnalyticsDashboardHandler(
        IAnalyticsRepository analyticsRepository,
        ILogger<GetAnalyticsDashboardHandler> logger)
    {
        _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AnalyticsDashboardDto> Handle(
        GetAnalyticsDashboardQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching operator analytics dashboard data");

        // Execute all analytics queries in parallel for performance
        var activeUsersTask = _analyticsRepository.GetActiveUsersAsync(cancellationToken);
        var featureHeatmapTask = _analyticsRepository.GetFeatureHeatmapAsync(ct: cancellationToken);
        var errorHotspotsTask = _analyticsRepository.GetErrorHotspotsAsync(ct: cancellationToken);
        var retentionTask = _analyticsRepository.GetRetentionMetricsAsync(cancellationToken);

        await Task.WhenAll(activeUsersTask, featureHeatmapTask, errorHotspotsTask, retentionTask);

        var dashboard = new AnalyticsDashboardDto(
            ActiveUsers: await activeUsersTask,
            FeatureHeatmap: await featureHeatmapTask,
            ErrorHotspots: await errorHotspotsTask,
            Retention: await retentionTask);

        _logger.LogInformation(
            "Analytics dashboard loaded: DAU={Dau}, WAU={Wau}, MAU={Mau}",
            dashboard.ActiveUsers.DailyActiveUsers,
            dashboard.ActiveUsers.WeeklyActiveUsers,
            dashboard.ActiveUsers.MonthlyActiveUsers);

        return dashboard;
    }
}
