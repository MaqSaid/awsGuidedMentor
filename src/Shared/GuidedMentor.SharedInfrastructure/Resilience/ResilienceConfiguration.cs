using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace GuidedMentor.SharedInfrastructure.Resilience;

/// <summary>
/// Configures Polly v8 resilience pipelines for external dependencies (Bedrock, DynamoDB, Aurora).
/// </summary>
public static class ResilienceConfiguration
{
    /// <summary>
    /// Registers all named resilience pipelines with the DI container.
    /// </summary>
    public static IServiceCollection AddGuidedMentorResilience(this IServiceCollection services)
    {
        services.AddResiliencePipeline(ResiliencePipelineNames.Bedrock, ConfigureBedrockPipeline);
        services.AddResiliencePipeline(ResiliencePipelineNames.DynamoDb, ConfigureDynamoDbPipeline);
        services.AddResiliencePipeline(ResiliencePipelineNames.Aurora, ConfigureAuroraPipeline);

        return services;
    }

    /// <summary>
    /// Bedrock pipeline: 30s timeout → retry 3x (2s, 4s, 8s exponential + jitter)
    /// → circuit breaker (50% fail ratio, 30s sample, 5 min throughput, 60s break).
    /// </summary>
    private static void ConfigureBedrockPipeline(ResiliencePipelineBuilder builder)
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
    /// DynamoDB pipeline: 5s timeout → retry 3x (200ms exponential + jitter),
    /// handles ProvisionedThroughputExceededException and InternalServerErrorException.
    /// </summary>
    private static void ConfigureDynamoDbPipeline(ResiliencePipelineBuilder builder)
    {
        builder
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<ProvisionedThroughputExceededException>()
                    .Handle<InternalServerErrorException>(),
            });
    }

    /// <summary>
    /// Aurora pipeline: 10s timeout → retry 3x (500ms exponential + jitter).
    /// </summary>
    private static void ConfigureAuroraPipeline(ResiliencePipelineBuilder builder)
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
