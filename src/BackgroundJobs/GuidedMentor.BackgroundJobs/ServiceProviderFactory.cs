using GuidedMentor.BackgroundJobs.Commands;
using GuidedMentor.Mentoring.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs;

/// <summary>
/// Creates and caches the DI service provider for Lambda function cold starts.
/// Lambda reuses the same container instance across warm invocations.
/// </summary>
public static class ServiceProviderFactory
{
    private static IServiceProvider? _serviceProvider;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets or creates the singleton ServiceProvider.
    /// Thread-safe for concurrent Lambda invocations during cold start.
    /// </summary>
    public static IServiceProvider Create()
    {
        if (_serviceProvider is not null)
            return _serviceProvider;

        lock (_lock)
        {
            if (_serviceProvider is not null)
                return _serviceProvider;

            var services = new ServiceCollection();

            // Register MediatR handlers from both Mentoring Application and BackgroundJobs assemblies
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(MentoringApplicationMarker.Assembly);
                cfg.RegisterServicesFromAssembly(typeof(ServiceProviderFactory).Assembly);
            });

            // Register logging (CloudWatch via Lambda runtime)
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddConsole();
            });

            // Register infrastructure dependencies
            // NOTE: In production, these registrations would include DynamoDB clients,
            // Aurora connection pools, EventBridge clients, etc.
            // The actual infrastructure registration is handled by the deployment configuration.
            services.AddBackgroundJobInfrastructure();

            _serviceProvider = services.BuildServiceProvider();
        }

        return _serviceProvider;
    }
}
