using Amazon.BedrockRuntime;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace GuidedMentor.SharedInfrastructure.HealthChecks;

/// <summary>
/// Extension methods for registering health checks per microservice.
/// Each service registers checks for its specific dependencies.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds a DynamoDB table health check.
    /// </summary>
    public static IHealthChecksBuilder AddDynamoDbCheck(
        this IHealthChecksBuilder builder,
        string tableName,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name ?? $"dynamodb-{tableName}",
            sp =>
            {
                var client = sp.GetRequiredService<IAmazonDynamoDB>();
                var logger = sp.GetRequiredService<ILogger<DynamoDbHealthCheck>>();
                return new DynamoDbHealthCheck(client, tableName, logger);
            },
            failureStatus,
            tags));
    }

    /// <summary>
    /// Adds a Bedrock Runtime health check.
    /// </summary>
    public static IHealthChecksBuilder AddBedrockCheck(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name ?? "bedrock-runtime",
            sp =>
            {
                var client = sp.GetRequiredService<IAmazonBedrockRuntime>();
                var logger = sp.GetRequiredService<ILogger<BedrockHealthCheck>>();
                return new BedrockHealthCheck(client, logger);
            },
            failureStatus,
            tags));
    }

    /// <summary>
    /// Adds an Aurora PostgreSQL health check.
    /// Only registers if NpgsqlDataSource is available in DI.
    /// </summary>
    public static IHealthChecksBuilder AddAuroraCheck(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name ?? "aurora-postgresql",
            sp =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                var logger = sp.GetRequiredService<ILogger<AuroraHealthCheck>>();
                return new AuroraHealthCheck(dataSource, logger);
            },
            failureStatus,
            tags));
    }

    /// <summary>
    /// Maps the /v1/health endpoint using ASP.NET Core health checks infrastructure.
    /// Returns 200 when all checks pass, 503 when any check is unhealthy.
    /// Response includes individual check statuses for diagnostics.
    /// </summary>
    public static WebApplication MapHealthCheckEndpoint(this WebApplication app)
    {
        app.MapHealthChecks("/v1/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            }
        });

        return app;
    }

    private static async Task WriteHealthCheckResponse(HttpContext httpContext, HealthReport report)
    {
        httpContext.Response.ContentType = "application/json";

        var response = new
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = entry.Value.Description,
                Duration = entry.Value.Duration.TotalMilliseconds,
                Error = entry.Value.Exception?.Message,
            })
        };

        await httpContext.Response.WriteAsJsonAsync(response);
    }
}
