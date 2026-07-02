using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace GuidedMentor.SharedInfrastructure.Resilience;

/// <summary>
/// Configures Polly v8 resilience pipelines for external dependencies (PostgreSQL, external APIs).
/// </summary>
public static class ResilienceConfiguration
{
    /// <summary>
    /// Registers all named resilience pipelines with the DI container.
    /// </summary>
    public static IServiceCollection AddGuidedMentorResilience(this IServiceCollection services)
    {
        services.AddResiliencePipeline(ResiliencePipelineNames.Bedrock, ConfigureExternalApiPipeline);
        services.AddResiliencePipeline(ResiliencePipelineNames.DynamoDb, ConfigurePostgresPipeline);
        services.AddResiliencePipeline(ResiliencePipelineNames.Aurora, ConfigurePostgresPipeline);

        return services;
    }

    /// <summary>
    /// External API pipeline: 30s timeout → retry 3x (2s, 4s, 8s exponential + jitter)
    /// → circuit breaker (50% fail ratio, 30s sample, 5 min throughput, 60s break).
    /// </summary>
    private static void ConfigureExternalApiPipeline(ResiliencePipelineBuilder builder)
    {
        builder
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                UseJitter = true,
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(60),
            });
    }

    /// <summary>
    /// PostgreSQL pipeline: 10s timeout → retry 3x (500ms exponential + jitter).
    /// </summary>
    private static void ConfigurePostgresPipeline(ResiliencePipelineBuilder builder)
    {
        builder
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(10),
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(500),
                UseJitter = true,
            });
    }
}
