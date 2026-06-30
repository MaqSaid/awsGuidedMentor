namespace GuidedMentor.BackgroundJobs;

/// <summary>
/// Represents an EventBridge Scheduler invocation payload.
/// EventBridge Scheduler sends a standardised event envelope to Lambda.
/// </summary>
public sealed record ScheduledEvent
{
    /// <summary>The scheduled event version (default: "0").</summary>
    public string Version { get; init; } = "0";

    /// <summary>The unique event ID assigned by EventBridge.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The source of the event (e.g., "aws.scheduler").</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>The AWS account ID.</summary>
    public string Account { get; init; } = string.Empty;

    /// <summary>The time the event was generated (ISO 8601).</summary>
    public string Time { get; init; } = string.Empty;

    /// <summary>The AWS region.</summary>
    public string Region { get; init; } = string.Empty;

    /// <summary>The detail-type of the event.</summary>
    public string DetailType { get; init; } = string.Empty;

    /// <summary>Resources associated with the event.</summary>
    public List<string> Resources { get; init; } = [];
}
