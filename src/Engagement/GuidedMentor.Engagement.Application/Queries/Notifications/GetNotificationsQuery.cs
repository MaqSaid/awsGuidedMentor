using GuidedMentor.Engagement.Application.DTOs;
using MediatR;

namespace GuidedMentor.Engagement.Application.Queries.Notifications;

/// <summary>
/// Returns the last N notifications for a user, reverse chronological order, unread distinguished.
/// Uses GSI-Recipient (PK=recipientUserId, SK=createdAt) with ScanIndexForward=false.
/// </summary>
public sealed record GetNotificationsQuery(Guid UserId, int Limit = 50) : IRequest<IReadOnlyList<NotificationDto>>;
