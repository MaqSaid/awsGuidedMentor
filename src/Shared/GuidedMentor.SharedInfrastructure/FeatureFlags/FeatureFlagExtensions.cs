using Amazon.AppConfigData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GuidedMentor.SharedInfrastructure.FeatureFlags;

/// <summary>
/// Extension methods for registering the AWS AppConfig feature flag service.
/// </summary>
public static class FeatureFlagExtensions
{
    /// <summary>
    /// Registers the AWS AppConfig feature flag service as a singleton.
    /// Reads configuration from the "AppConfig" section.
    /// </summary>
    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AppConfigOptions>(configuration.GetSection(AppConfigOptions.SectionName));
        services.AddSingleton<IAmazonAppConfigData, AmazonAppConfigDataClient>();
        services.AddSingleton<IFeatureFlagService, AppConfigFeatureFlagService>();

        return services;
    }
}
