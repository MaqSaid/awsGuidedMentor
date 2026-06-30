using System.Diagnostics.Metrics;

namespace GuidedMentor.Observability.Metrics;

/// <summary>
/// Custom metrics for tracking API request latency and error counts.
/// Provides percentile-based latency tracking and error categorization by endpoint.
/// </summary>
public sealed class ApiMetrics
{
    public const string MeterName = "GuidedMentor.Api";

    private readonly Histogram<double> _latency;
    private readonly Counter<long> _errors;

    public ApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _latency = meter.CreateHistogram<double>(
            "api_latency",
            unit: "ms",
            description: "API request latency in milliseconds");

        _errors = meter.CreateCounter<long>(
            "api_errors",
            unit: "errors",
            description: "Count of API errors by endpoint and status code");
    }

    /// <summary>
    /// Records API request latency for a given endpoint and HTTP method.
    /// </summary>
    public void RecordLatency(double milliseconds, string endpoint, string method)
    {
        _latency.Record(milliseconds,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method));
    }

    /// <summary>
    /// Records an API error occurrence for a given endpoint and status code.
    /// </summary>
    public void RecordError(string endpoint, int statusCode, string method)
    {
        _errors.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status_code", statusCode),
            new KeyValuePair<string, object?>("method", method));
    }
}
