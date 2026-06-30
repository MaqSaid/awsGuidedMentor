using GuidedMentor.Engagement.Application.Analytics.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Handles conversion funnel data retrieval for the operator analytics dashboard.
/// Queries Aurora PostgreSQL for user progression through the platform funnel:
/// signup → onboard → browse → match → session → complete.
///
/// Admin-only access enforced at the API layer.
///
/// Requirements: 30.6
/// </summary>
public sealed class GetFunnelDataHandler : IRequestHandler<GetFunnelDataQuery, FunnelDataDto>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ILogger<GetFunnelDataHandler> _logger;

    public GetFunnelDataHandler(
        IAnalyticsRepository analyticsRepository,
        ILogger<GetFunnelDataHandler> logger)
    {
        _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FunnelDataDto> Handle(
        GetFunnelDataQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching conversion funnel data");

        var funnelData = await _analyticsRepository.GetFunnelDataAsync(cancellationToken);

        _logger.LogInformation(
            "Funnel data loaded: {StageCount} stages, overall conversion={ConversionRate:F1}%",
            funnelData.Stages.Count,
            funnelData.OverallConversionRate);

        return funnelData;
    }
}
