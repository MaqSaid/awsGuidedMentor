using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Domain.Repositories;

/// <summary>
/// Repository interface for Notification persistence (DynamoDB Notifications_Table).
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Persists a new notification to the Notifications_Table.
    /// </summary>
    Task SaveAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification by its ID.
    /// </summary>
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent notifications for a user, ordered by createdAt descending.
    /// Uses GSI-Recipient (PK=recipientUserId, SK=createdAt).
    /// </summary>
    Task<IReadOnlyList<Notification>> GetByRecipientAsync(
        UserId recipientUserId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the count of unread notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(UserId recipientUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a single notification as read.
    /// </summary>
    Task MarkAsReadAsync(NotificationId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all unread notifications for a user as read (batch operation).
    /// </summary>
    Task BatchMarkAsReadAsync(UserId recipientUserId, CancellationToken cancellationToken = default);
}
