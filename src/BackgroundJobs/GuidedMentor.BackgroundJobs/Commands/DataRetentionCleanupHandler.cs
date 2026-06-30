using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Handles the DataRetentionCleanupCommand by scanning for inactive accounts
/// and enforcing the data retention policy.
///
/// Implements ISO 27001 data retention requirements:
/// 1. Scan Users_Table for lastActivityAt older than retention period (3 years)
/// 2. Send 30-day warning notification for accounts approaching deletion
/// 3. After grace period: execute full deletion across all data stores
/// 4. Process pending user deletion requests (must complete within 30 days)
/// 5. Log all actions to audit trail
///
/// Requirements: 21.16
/// </summary>
public sealed class DataRetentionCleanupHandler : IRequestHandler<DataRetentionCleanupCommand, Result<DataRetentionResult>>
{
    private readonly IDataRetentionService _retentionService;
    private readonly ILogger<DataRetentionCleanupHandler> _logger;

    public DataRetentionCleanupHandler(
        IDataRetentionService retentionService,
        ILogger<DataRetentionCleanupHandler> logger)
    {
        _retentionService = retentionService ?? throw new ArgumentNullException(nameof(retentionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DataRetentionResult>> Handle(
        DataRetentionCleanupCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting data retention cleanup. RetentionYears={RetentionYears}, GracePeriodDays={GracePeriodDays}",
            request.RetentionYears, request.GracePeriodDays);

        var warningsSent = 0;
        var deletionsExecuted = 0;
        var deletionRequestsProcessed = 0;

        try
        {
            // Step 1: Find inactive users past retention threshold
            var retentionCutoff = DateTime.UtcNow.AddYears(-request.RetentionYears);
            var inactiveUsers = await _retentionService.GetInactiveUsersSinceAsync(
                retentionCutoff, cancellationToken);

            _logger.LogInformation(
                "Found {InactiveCount} users inactive since {Cutoff:O}",
                inactiveUsers.Count, retentionCutoff);

            // Step 2: For each inactive user, send warning or delete
            var graceCutoff = DateTime.UtcNow.AddDays(-request.GracePeriodDays);

            foreach (var user in inactiveUsers)
            {
                if (!user.RetentionWarningsentAt.HasValue)
                {
                    // First time: send 30-day warning
                    await _retentionService.SendRetentionWarningAsync(user.UserId, cancellationToken);
                    warningsSent++;
                }
                else if (user.RetentionWarningsentAt.Value < graceCutoff)
                {
                    // Grace period expired: execute full deletion
                    await _retentionService.ExecuteFullDeletionAsync(user.UserId, cancellationToken);
                    deletionsExecuted++;
                }
            }

            // Step 3: Process pending user-initiated deletion requests
            var pendingDeletions = await _retentionService.GetPendingDeletionRequestsAsync(cancellationToken);

            foreach (var deletion in pendingDeletions)
            {
                await _retentionService.ExecuteFullDeletionAsync(deletion.UserId, cancellationToken);
                deletionRequestsProcessed++;
            }

            var result = new DataRetentionResult(
                UsersScanned: inactiveUsers.Count,
                WarningsSent: warningsSent,
                DeletionsExecuted: deletionsExecuted,
                DeletionRequestsProcessed: deletionRequestsProcessed);

            _logger.LogInformation(
                "Data retention cleanup completed. Scanned={Scanned}, Warnings={Warnings}, Deletions={Deletions}, Requests={Requests}",
                result.UsersScanned, result.WarningsSent, result.DeletionsExecuted, result.DeletionRequestsProcessed);

            return Result<DataRetentionResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data retention cleanup encountered an error.");
            return Result<DataRetentionResult>.Failure($"Data retention cleanup failed: {ex.Message}");
        }
    }
}
