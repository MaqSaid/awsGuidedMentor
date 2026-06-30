namespace GuidedMentor.Engagement.Infrastructure.RealTime;

/// <summary>
/// Configuration options for the AWS AppSync real-time notification endpoint.
/// </summary>
public sealed class AppSyncOptions
{
    public const string SectionName = "AppSync";

    /// <summary>
    /// The AppSync GraphQL HTTPS endpoint URL.
    /// </summary>
    public string GraphqlEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// The AppSync API key (used for server-to-server notification publishing).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The AppSync real-time WebSocket endpoint URL (used by clients for subscriptions).
    /// </summary>
    public string RealtimeEndpoint { get; set; } = string.Empty;
}
