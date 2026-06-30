namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Manages platform feature flags and maintenance mode via AppConfig.
/// Allows Super_Admins to enable/disable features without deployment.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Gets whether the platform is currently in maintenance mode.
    /// </summary>
    Task<bool> IsMaintenanceModeEnabledAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets maintenance mode on or off, with optional estimated return time.
    /// </summary>
    Task SetMaintenanceModeAsync(bool enabled, string? estimatedReturnTime, CancellationToken ct = default);

    /// <summary>
    /// Gets the current state of a feature flag.
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken ct = default);

    /// <summary>
    /// Enables or disables a specific feature flag.
    /// </summary>
    Task SetFeatureEnabledAsync(string featureName, bool enabled, CancellationToken ct = default);
}
