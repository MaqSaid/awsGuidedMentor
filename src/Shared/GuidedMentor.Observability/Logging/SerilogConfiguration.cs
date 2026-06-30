using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace GuidedMentor.Observability.Logging;

/// <summary>
/// Configures Serilog structured logging with JSON formatter for CloudWatch Logs.
/// Provides enrichers for correlationId, userId, requestPath, duration, service, and environment.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Adds GuidedMentor structured logging with Serilog to the service collection.
    /// Configures JSON output suitable for CloudWatch Logs ingestion.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">The name of the calling microservice (e.g., "Identity.Api").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGuidedMentorLogging(this IServiceCollection services, string serviceName)
    {
        var minimumLevel = GetMinimumLogLevel();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("Environment", environment)
            .Enrich.With<CorrelationIdEnricher>()
            .Enrich.With<UserIdEnricher>()
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            .CreateLogger();

        services.AddSerilog();

        return services;
    }

    private static LogEventLevel GetMinimumLogLevel()
    {
        var levelStr = Environment.GetEnvironmentVariable("LOG_MINIMUM_LEVEL");

        if (string.IsNullOrWhiteSpace(levelStr))
        {
            return LogEventLevel.Information;
        }

        return Enum.TryParse<LogEventLevel>(levelStr, ignoreCase: true, out var level)
            ? level
            : LogEventLevel.Information;
    }
}
