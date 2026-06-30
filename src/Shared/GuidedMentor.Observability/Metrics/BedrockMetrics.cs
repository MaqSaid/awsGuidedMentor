using System.Diagnostics.Metrics;

namespace GuidedMentor.Observability.Metrics;

/// <summary>
/// Custom metrics for tracking Amazon Bedrock usage including token consumption and latency.
/// Used for cost monitoring and performance analysis of AI inference calls.
/// </summary>
public sealed class BedrockMetrics
{
    public const string MeterName = "GuidedMentor.Bedrock";

    private readonly Counter<long> _inputTokens;
    private readonly Counter<long> _outputTokens;
    private readonly Histogram<double> _latency;

    public BedrockMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _inputTokens = meter.CreateCounter<long>(
            "bedrock_tokens_input",
            unit: "tokens",
            description: "Number of input tokens sent to Bedrock");

        _outputTokens = meter.CreateCounter<long>(
            "bedrock_tokens_output",
            unit: "tokens",
            description: "Number of output tokens received from Bedrock");

        _latency = meter.CreateHistogram<double>(
            "bedrock_latency",
            unit: "ms",
            description: "Latency of Bedrock inference calls in milliseconds");
    }

    /// <summary>
    /// Records input token usage for a Bedrock call.
    /// </summary>
    public void RecordInputTokens(long tokens, string model = "claude-sonnet-4")
    {
        _inputTokens.Add(tokens, new KeyValuePair<string, object?>("model", model));
    }

    /// <summary>
    /// Records output token usage for a Bedrock call.
    /// </summary>
    public void RecordOutputTokens(long tokens, string model = "claude-sonnet-4")
    {
        _outputTokens.Add(tokens, new KeyValuePair<string, object?>("model", model));
    }

    /// <summary>
    /// Records the latency of a Bedrock inference call.
    /// </summary>
    public void RecordLatency(double milliseconds, string model = "claude-sonnet-4")
    {
        _latency.Record(milliseconds, new KeyValuePair<string, object?>("model", model));
    }
}
