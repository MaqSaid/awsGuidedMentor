namespace GuidedMentor.SharedInfrastructure.FeatureFlags;

/// <summary>
/// Abstraction for checking feature flag state at runtime.
/// Backed by AWS AppConfig for progressive rollouts and canary deployments.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks whether a named feature is currently enabled.
    /// </summary>
    /// <param name="featureName">The feature flag name (e.g., "AiHelp", "JobBoard").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the feature is enabled for the current deployment percentage.</returns>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current rollout percentage for a feature (0-100).
    /// Used for canary deployment progression: 1% → 10% → 50% → 100%.
    /// </summary>
    /// <param name="featureName">The feature flag name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rollout percentage (0-100), or null if the feature is not configured.</returns>
    Task<int?> GetRolloutPercentageAsync(string featureName, CancellationToken cancellationToken = default);
}
