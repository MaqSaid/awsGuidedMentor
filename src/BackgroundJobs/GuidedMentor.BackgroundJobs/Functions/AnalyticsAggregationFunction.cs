using Amazon.Lambda.Core;
using GuidedMentor.BackgroundJobs.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Lambda function triggered hourly by EventBridge Scheduler.
/// Aggregates raw engagement events from DynamoDB into Aurora PostgreSQL
/// materialized views/summary tables for the analytics dashboard.
/// Only runs when Aurora is enabled (staging/prod).
///
/// Requirements: 20.7
/// </summary>
public sealed class AnalyticsAggregationFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnalyticsAggregationFunction> _logger;

    public AnalyticsAggregationFunction()
    {
        var services = ServiceProviderFactory.Create();
        _mediator = services.GetRequiredService<IMediator>();
        _logger = services.GetRequiredService<ILogger<AnalyticsAggregationFunction>>();
    }

    /// <summary>
    /// Entry point invoked hourly by EventBridge Scheduler.
    /// Dispatches AggregateAnalyticsCommand via MediatR.
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task HandleAsync(ScheduledEvent scheduledEvent, ILambdaContext context)
    {
        _logger.LogInformation(
            "Analytics aggregation triggered. RequestId: {RequestId}",
            context.AwsRequestId);

        try
        {
            var command = new AggregateAnalyticsCommand();
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Analytics aggregation completed successfully.");
            }
            else
            {
                _logger.LogWarning("Analytics aggregation completed with error: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analytics aggregation failed.");
            throw;
        }
    }
}
