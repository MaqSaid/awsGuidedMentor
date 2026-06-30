using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Notifications;

/// <summary>
/// Marks a single notification as read.
/// </summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;
