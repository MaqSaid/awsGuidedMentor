using Amazon.Lambda.Core;
using GuidedMentor.Mentoring.Application.Commands.Locking;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Lambda function triggered every 5 minutes by EventBridge Scheduler.
/// Cleans up expired mentor locks that were not explicitly released.
/// While DynamoDB TTL handles eventual cleanup, this function ensures
/// locks are released promptly for real-time browse availability.
///
/// Requirements: 20.7
/// </summary>
public sealed class LockExpirationCleanupFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<LockExpirationCleanupFunction> _logger;

    public LockExpirationCleanupFunction()
    {
        var services = ServiceProviderFactory.Create();
        _mediator = services.GetRequiredService<IMediator>();
        _logger = services.GetRequiredService<ILogger<LockExpirationCleanupFunction>>();
    }

    /// <summary>
    /// Entry point invoked by EventBridge Scheduler (rate: 5 minutes).
    /// Dispatches CleanupExpiredLocksCommand via MediatR.
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task HandleAsync(ScheduledEvent scheduledEvent, ILambdaContext context)
    {
        _logger.LogInformation(
            "Lock expiration cleanup triggered. RequestId: {RequestId}",
            context.AwsRequestId);

        try
        {
            var command = new CleanupExpiredLocksCommand();
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Lock expiration cleanup completed successfully.");
            }
            else
            {
                _logger.LogWarning("Lock expiration cleanup completed with error: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lock expiration cleanup failed.");
            throw;
        }
    }
}
