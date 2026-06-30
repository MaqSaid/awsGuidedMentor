using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Notifications;

/// <summary>
/// Creates a notification, persists it to the Notifications_Table, and pushes via AppSync subscription.
/// </summary>
public sealed record CreateNotificationCommand(
    Guid RecipientUserId,
    NotificationType Type,
    string Message,
    string ActionUrl) : IRequest<Result>;
