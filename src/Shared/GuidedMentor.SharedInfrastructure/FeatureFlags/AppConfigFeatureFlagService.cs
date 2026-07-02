using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.FeatureFlags;

/// <summary>
/// Configuration-backed feature flag service. Replaces AWS AppConfig.
/// Reads feature flags from appsettings.json "FeatureFlags" section.
/// </summary>
public sealed class ConfigFeatureFlagService : IFeatureFlagService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigFeatureFlagService> _logger;

    public ConfigFeatureFlagService(
        IConfiguration configuration,
        ILogger<ConfigFeatureFlagService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var section = _configuration.GetSection("FeatureFlags");
        var value = section[featureName];

        if (value is null)
        {
            _logger.LogWarning("Feature flag '{FeatureName}' not found in configuration", featureName);
            return Task.FromResult(false);
        }

        return Task.FromResult(bool.TryParse(value, out var enabled) && enabled);
    }

    public Task<int?> GetRolloutPercentageAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var section = _configuration.GetSection("FeatureFlags");
        var value = section[featureName];

        if (value is null)
            return Task.FromResult<int?>(null);

        // If the flag is a bool true, treat as 100%
        if (bool.TryParse(value, out var enabled))
            return Task.FromResult<int?>(enabled ? 100 : 0);

        // If it's a number, treat as percentage
        if (int.TryParse(value, out var pct))
            return Task.FromResult<int?>(pct);

        return Task.FromResult<int?>(null);
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}
