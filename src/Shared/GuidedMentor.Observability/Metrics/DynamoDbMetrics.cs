using System.Diagnostics.Metrics;

namespace GuidedMentor.Observability.Metrics;

/// <summary>
/// Custom metrics for tracking DynamoDB consumed capacity units.
/// Helps monitor capacity usage patterns and detect throttling risks.
/// </summary>
public sealed class DynamoDbMetrics
{
    public const string MeterName = "GuidedMentor.DynamoDb";

    private readonly Counter<double> _consumedCapacity;
    private readonly Histogram<double> _computationTime;

    public DynamoDbMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _consumedCapacity = meter.CreateCounter<double>(
            "dynamodb_consumed_capacity",
            unit: "RCU/WCU",
            description: "DynamoDB consumed capacity units");

        _computationTime = meter.CreateHistogram<double>(
            "matching_computation_time",
            unit: "ms",
            description: "Time to compute matching scores in milliseconds");
    }

    /// <summary>
    /// Records DynamoDB consumed capacity for a given table and operation type.
    /// </summary>
    public void RecordConsumedCapacity(double capacityUnits, string tableName, string operationType)
    {
        _consumedCapacity.Add(capacityUnits,
            new KeyValuePair<string, object?>("table", tableName),
            new KeyValuePair<string, object?>("operation", operationType));
    }

    /// <summary>
    /// Records the time taken for matching computation.
    /// </summary>
    public void RecordMatchingComputationTime(double milliseconds)
    {
        _computationTime.Record(milliseconds);
    }
}
