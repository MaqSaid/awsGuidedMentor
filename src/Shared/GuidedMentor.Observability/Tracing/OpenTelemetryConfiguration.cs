using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GuidedMentor.Observability.Tracing;

/// <summary>
/// Configures OpenTelemetry SDK with X-Ray trace ID format, OTLP exporter,
/// and custom metrics for GuidedMentor services.
/// </summary>
public static class OpenTelemetryConfiguration
{
    /// <summary>
    /// Adds GuidedMentor OpenTelemetry tracing and metrics to the service collection.
    /// Configures X-Ray-compatible trace ID format and OTLP exporter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">The name of the calling microservice (e.g., "Identity.Api").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGuidedMentorTracing(this IServiceCollection services, string serviceName)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: serviceName,
                    serviceVersion: "1.0.0");
                resource.AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                    ["cloud.provider"] = "aws",
                    ["cloud.region"] = Environment.GetEnvironmentVariable("AWS_REGION") ?? "ap-southeast-2"
                });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
                }
                else
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(Metrics.BedrockMetrics.MeterName)
                    .AddMeter(Metrics.ApiMetrics.MeterName)
                    .AddMeter(Metrics.DynamoDbMetrics.MeterName);

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
                }
                else
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }
}
