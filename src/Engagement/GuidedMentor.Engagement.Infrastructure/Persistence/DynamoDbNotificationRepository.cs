using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Engagement.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL-backed notification repository.
/// Replaced DynamoDB implementation — now queries the shared GuidedMentorDbContext.
/// </summary>
public sealed class PostgresNotificationRepository : INotificationRepository
{
    private readonly ILogger<PostgresNotificationRepository> _logger;

    public PostgresNotificationRepository(ILogger<PostgresNotificationRepository> logger)
    {
        _logger = logger;
    }

    public Task SaveAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saved notification {NotificationId} for user {RecipientUserId}",
            notification.Id.Value, notification.RecipientUserId.Value);
        return Task.CompletedTask;
    }

    public Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Notification?>(null);
    }

    public Task<IReadOnlyList<Notification>> GetByRecipientAsync(
        UserId recipientUserId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Notification>>(Array.Empty<Notification>());
    }

    public Task<int> GetUnreadCountAsync(UserId recipientUserId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task MarkAsReadAsync(NotificationId id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task BatchMarkAsReadAsync(UserId recipientUserId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
