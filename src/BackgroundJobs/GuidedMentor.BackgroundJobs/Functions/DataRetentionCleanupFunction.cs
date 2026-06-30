using Amazon.Lambda.Core;
using GuidedMentor.BackgroundJobs.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Functions;

/// <summary>
/// Lambda function triggered daily at 02:00 AEST by EventBridge Scheduler.
/// Enforces data retention policy per ISO 27001 compliance:
/// - Identifies user accounts inactive for 3+ years (based on lastActivityAt)
/// - Sends 30-day warning notifications for accounts approaching deletion
/// - Executes full data deletion for accounts past the grace period
/// - Anonymises analytics records for deleted users
///
/// Requirements: 21.16 (ISO 27001 data retention policy)
/// </summary>
public sealed class DataRetentionCleanupFunction
{
    private readonly IMediator _mediator;
    private readonly ILogger<DataRetentionCleanupFunction> _logger;

    public DataRetentionCleanupFunction()
    {
        var services = ServiceProviderFactory.Create();
        _mediator = services.GetRequiredService<IMediator>();
        _logger = services.GetRequiredService<ILogger<DataRetentionCleanupFunction>>();
    }

    /// <summary>
    /// Entry point invoked by EventBridge Scheduler (daily at 02:00 AEST).
    /// Dispatches DataRetentionCleanupCommand via MediatR.
    /// </summary>
    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task HandleAsync(ScheduledEvent scheduledEvent, ILambdaContext context)
    {
        _logger.LogInformation(
            "Data retention cleanup triggered. RequestId: {RequestId}",
            context.AwsRequestId);

        try
        {
            var command = new DataRetentionCleanupCommand(
                RetentionYears: 3,
                GracePeriodDays: 30,
                DeletionRequestMaxDays: 30);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Data retention cleanup completed. Warnings sent: {Warnings}, Deletions: {Deletions}",
                    result.Value.WarningsSent, result.Value.DeletionsExecuted);
            }
            else
            {
                _logger.LogWarning(
                    "Data retention cleanup completed with error: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data retention cleanup failed.");
            throw;
        }
    }
}
