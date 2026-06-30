namespace GuidedMentor.Content.Application.Interfaces;

/// <summary>
/// Publishes Bedrock token usage metrics to Amazon CloudWatch for cost monitoring.
/// Validates: Requirement 7.9, 24.5
/// </summary>
public interface IBedrockMetricsPublisher
{
    /// <summary>
    /// Publishes input and output token counts as custom CloudWatch metrics.
    /// Metric namespace: GuidedMentor/Bedrock
    /// Dimensions: Operation (e.g., "SessionPlanGeneration"), SessionId
    /// </summary>
    /// <param name="inputTokens">Number of input tokens consumed by the Bedrock request.</param>
    /// <param name="outputTokens">Number of output tokens produced by the Bedrock response.</param>
    /// <param name="sessionId">The session ID for metric dimensions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishTokenUsageAsync(
        int inputTokens,
        int outputTokens,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
