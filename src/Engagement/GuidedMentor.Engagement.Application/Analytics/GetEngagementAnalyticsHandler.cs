using GuidedMentor.Engagement.Application.Analytics.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Handles engagement-specific analytics queries for the operator dashboard.
/// Queries Aurora PostgreSQL for:
/// - Browse-to-lock conversion rate (mentor browse → lock confirmation)
/// - Plan-to-completion rate (session plan view → checklist completion)
/// - Job view-to-click rate (opportunity view → apply/register click)
///
/// Admin-only access enforced at the API layer.
///
/// Requirements: 30.5
/// </summary>
public sealed class GetEngagementAnalyticsHandler
    : IRequestHandler<GetEngagementAnalyticsQuery, EngagementAnalyticsDto>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ILogger<GetEngagementAnalyticsHandler> _logger;

    public GetEngagementAnalyticsHandler(
        IAnalyticsRepository analyticsRepository,
        ILogger<GetEngagementAnalyticsHandler> logger)
    {
        _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EngagementAnalyticsDto> Handle(
        GetEngagementAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Fetching engagement analytics from={From} to={To}",
            request.From,
            request.To);

        var analytics = await _analyticsRepository.GetEngagementAnalyticsAsync(
            request.From,
            request.To,
            cancellationToken);

        _logger.LogInformation(
            "Engagement analytics loaded: browse-to-lock={BrowseToLock:F1}%, plan-to-complete={PlanToComplete:F1}%, job-view-to-click={JobClick:F1}%",
            analytics.BrowseToLockConversionRate,
            analytics.PlanToCompletionRate,
            analytics.JobViewToClickRate);

        return analytics;
    }
}
