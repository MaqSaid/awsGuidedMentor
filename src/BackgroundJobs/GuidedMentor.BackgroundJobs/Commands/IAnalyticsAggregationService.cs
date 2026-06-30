namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Service interface for analytics aggregation.
/// Implementation reads raw engagement events from DynamoDB EngagementEvents_Table
/// and materialises summary metrics into Aurora PostgreSQL tables.
/// </summary>
public interface IAnalyticsAggregationService
{
    /// <summary>
    /// Aggregates raw engagement events since the last aggregation timestamp.
    /// Computes and persists: active user counts (DAU/WAU/MAU), feature heatmap,
    /// error hotspots, retention metrics, and conversion funnel stages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of raw events processed during this aggregation run.</returns>
    Task<int> AggregateEventsAsync(CancellationToken cancellationToken = default);
}
