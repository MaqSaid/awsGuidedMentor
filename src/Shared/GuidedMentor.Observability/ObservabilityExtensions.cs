using GuidedMentor.Observability.Logging;
using GuidedMentor.Observability.Metrics;
using GuidedMentor.Observability.Middleware;
using GuidedMentor.Observability.Tracing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GuidedMentor.Observability;

/// <summary>
/// Provides extension methods to register all observability services
/// (logging, tracing, metrics, middleware) in a single call.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Adds all GuidedMentor observability services: Serilog logging, OpenTelemetry tracing, and custom metrics.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">The name of the calling microservice.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGuidedMentorObservability(this IServiceCollection services, string serviceName)
    {
        services.AddGuidedMentorLogging(serviceName);
        services.AddGuidedMentorTracing(serviceName);

        // Register custom metric publishers as singletons
        services.AddSingleton<BedrockMetrics>();
        services.AddSingleton<ApiMetrics>();
        services.AddSingleton<DynamoDbMetrics>();

        return services;
    }

    /// <summary>
    /// Adds the correlation ID middleware to the request pipeline.
    /// Should be registered early in the pipeline, before authentication and routing.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
