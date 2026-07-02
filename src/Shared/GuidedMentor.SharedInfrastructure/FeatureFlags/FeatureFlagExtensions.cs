using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GuidedMentor.SharedInfrastructure.FeatureFlags;

/// <summary>
/// Extension methods for registering the configuration-based feature flag service.
/// </summary>
public static class FeatureFlagExtensions
{
    /// <summary>
    /// Registers the configuration-based feature flag service as a singleton.
    /// Reads flags from the "FeatureFlags" section of appsettings.json.
    /// </summary>
    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IFeatureFlagService, ConfigFeatureFlagService>();
        return services;
    }
}
