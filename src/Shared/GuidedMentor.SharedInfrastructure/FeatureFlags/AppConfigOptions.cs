namespace GuidedMentor.SharedInfrastructure.FeatureFlags;

/// <summary>
/// Configuration options for the AWS AppConfig feature flag client.
/// </summary>
public sealed class AppConfigOptions
{
    public const string SectionName = "AppConfig";

    /// <summary>The AppConfig application identifier.</summary>
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>The AppConfig environment identifier (dev, staging, prod).</summary>
    public string EnvironmentId { get; set; } = string.Empty;

    /// <summary>The AppConfig configuration profile identifier for feature flags.</summary>
    public string ConfigurationProfileId { get; set; } = string.Empty;

    /// <summary>
    /// Polling interval for configuration updates in seconds.
    /// AppConfig data is cached locally and refreshed at this interval.
    /// Default: 45 seconds (matches AppConfig minimum polling interval).
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 45;
}
