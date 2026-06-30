using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Handles hourly analytics aggregation.
/// Reads raw events from DynamoDB and materialises aggregated metrics
/// into Aurora PostgreSQL for the operator analytics dashboard.
/// </summary>
public sealed class AggregateAnalyticsHandler : IRequestHandler<AggregateAnalyticsCommand, Result>
{
    private readonly IAnalyticsAggregationService _aggregationService;
    private readonly ILogger<AggregateAnalyticsHandler> _logger;

    public AggregateAnalyticsHandler(
        IAnalyticsAggregationService aggregationService,
        ILogger<AggregateAnalyticsHandler> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    public async Task<Result> Handle(AggregateAnalyticsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting hourly analytics aggregation.");

        try
        {
            var aggregatedCount = await _aggregationService.AggregateEventsAsync(cancellationToken);

            _logger.LogInformation(
                "Analytics aggregation completed. {Count} events processed.",
                aggregatedCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analytics aggregation failed.");
            return Result.Failure($"Analytics aggregation failed: {ex.Message}");
        }
    }
}
