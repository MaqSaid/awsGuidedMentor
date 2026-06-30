using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Domain.Entities;

/// <summary>
/// Represents a notification delivered to a user. Notifications are persisted
/// to the Notifications_Table and pushed in real-time via AppSync subscriptions.
/// </summary>
public sealed class Notification : Entity<NotificationId>
{
    public UserId RecipientUserId { get; private set; } = default!;
    public NotificationType Type { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string ActionUrl { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    /// <summary>
    /// Factory method to create a new notification with validation.
    /// </summary>
    public static Result<Notification> Create(
        UserId recipientUserId,
        NotificationType type,
        string message,
        string actionUrl)
    {
        if (recipientUserId is null)
            return Result<Notification>.Failure("Recipient user ID is required.");

        if (string.IsNullOrWhiteSpace(message))
            return Result<Notification>.Failure("Notification message is required.");

        if (message.Length > 500)
            return Result<Notification>.Failure("Notification message must not exceed 500 characters.");

        if (string.IsNullOrWhiteSpace(actionUrl))
            return Result<Notification>.Failure("Action URL is required.");

        var notification = new Notification
        {
            Id = NotificationId.New(),
            RecipientUserId = recipientUserId,
            Type = type,
            Message = message,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        return Result<Notification>.Success(notification);
    }

    /// <summary>
    /// Reconstitutes a notification from persistence (no validation, trusted data).
    /// </summary>
    public static Notification Reconstitute(
        NotificationId id,
        UserId recipientUserId,
        NotificationType type,
        string message,
        string actionUrl,
        bool isRead,
        DateTime createdAt)
    {
        return new Notification
        {
            Id = id,
            RecipientUserId = recipientUserId,
            Type = type,
            Message = message,
            ActionUrl = actionUrl,
            IsRead = isRead,
            CreatedAt = createdAt
        };
    }

    /// <summary>
    /// Marks this notification as read.
    /// </summary>
    public void MarkAsRead() => IsRead = true;
}
