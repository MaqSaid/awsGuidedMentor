using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Handles the daily notification digest.
/// Queries users with unread notifications from the past 24 hours,
/// aggregates them into a summary, and dispatches digest notifications.
/// </summary>
public sealed class SendNotificationDigestHandler : IRequestHandler<SendNotificationDigestCommand, Result>
{
    private readonly INotificationDigestService _digestService;
    private readonly ILogger<SendNotificationDigestHandler> _logger;

    public SendNotificationDigestHandler(
        INotificationDigestService digestService,
        ILogger<SendNotificationDigestHandler> logger)
    {
        _digestService = digestService;
        _logger = logger;
    }

    public async Task<Result> Handle(SendNotificationDigestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting daily notification digest (9 AM AEST).");

        try
        {
            var digestsSent = await _digestService.SendDigestsAsync(cancellationToken);

            _logger.LogInformation(
                "Notification digest completed. {Count} digest(s) sent.",
                digestsSent);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification digest processing failed.");
            return Result.Failure($"Notification digest failed: {ex.Message}");
        }
    }
}
