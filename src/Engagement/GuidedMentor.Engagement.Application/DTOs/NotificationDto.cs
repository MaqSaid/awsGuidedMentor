using GuidedMentor.Engagement.Domain.Entities;

namespace GuidedMentor.Engagement.Application.DTOs;

/// <summary>
/// Read-optimised DTO for notification display.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    Guid RecipientUserId,
    NotificationType Type,
    string Message,
    string ActionUrl,
    bool IsRead,
    DateTime CreatedAt);
