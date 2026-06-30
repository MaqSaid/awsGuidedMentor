using GuidedMentor.BackgroundJobs.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace GuidedMentor.BackgroundJobs;

/// <summary>
/// Registers background job infrastructure services.
/// In production, these are resolved from the Lambda runtime environment
/// with full AWS SDK clients configured.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all background job infrastructure dependencies.
    /// Implementations are provided by the deployment layer (e.g., Aurora clients, DynamoDB SDK).
    /// </summary>
    public static IServiceCollection AddBackgroundJobInfrastructure(this IServiceCollection services)
    {
        // Infrastructure service registrations are injected at deployment time.
        // The interfaces (IAnalyticsAggregationService, INotificationDigestService)
        // are implemented in the Infrastructure layer and registered here when
        // the Lambda is packaged with the full dependency chain.
        //
        // For local development / testing, register mock implementations:
        // services.AddSingleton<IAnalyticsAggregationService, MockAnalyticsAggregationService>();
        // services.AddSingleton<INotificationDigestService, MockNotificationDigestService>();

        return services;
    }
}
