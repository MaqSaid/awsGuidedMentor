using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.HealthChecks;

/// <summary>
/// Custom health check that verifies Amazon Bedrock Runtime availability
/// by invoking a lightweight model listing operation.
/// </summary>
public sealed class BedrockHealthCheck : IHealthCheck
{
    private readonly IAmazonBedrockRuntime _bedrockRuntime;
    private readonly ILogger<BedrockHealthCheck> _logger;

    public BedrockHealthCheck(
        IAmazonBedrockRuntime bedrockRuntime,
        ILogger<BedrockHealthCheck> logger)
    {
        _bedrockRuntime = bedrockRuntime;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a minimal Converse call with a tiny payload to verify connectivity.
            // We intentionally catch ModelNotReadyException as a sign the service is reachable but model is loading.
            var request = new ConverseRequest
            {
                ModelId = "anthropic.claude-sonnet-4-20250514-v1:0",
                Messages =
                [
                    new Message
                    {
                        Role = ConversationRole.User,
                        Content = [new ContentBlock { Text = "ping" }]
                    }
                ],
                InferenceConfig = new InferenceConfiguration
                {
                    MaxTokens = 1,
                    Temperature = 0F
                }
            };

            var response = await _bedrockRuntime.ConverseAsync(request, cancellationToken);

            return response.StopReason is not null
                ? HealthCheckResult.Healthy("Bedrock Runtime is available.")
                : HealthCheckResult.Healthy("Bedrock Runtime responded.");
        }
        catch (ModelNotReadyException)
        {
            return HealthCheckResult.Degraded("Bedrock model is loading.");
        }
        catch (ThrottlingException)
        {
            // Throttled but reachable — treat as degraded rather than unhealthy
            return HealthCheckResult.Degraded("Bedrock Runtime is throttled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bedrock health check failed");
            return HealthCheckResult.Unhealthy($"Bedrock Runtime unavailable: {ex.Message}");
        }
    }
}
