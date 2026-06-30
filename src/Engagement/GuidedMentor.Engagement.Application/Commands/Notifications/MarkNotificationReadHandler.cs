using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Notifications;

/// <summary>
/// Handles marking a single notification as read.
/// </summary>
public sealed class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly INotificationRepository _repository;

    public MarkNotificationReadHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notificationId = new NotificationId(request.NotificationId);

        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null)
        {
            return Result.Failure("Notification not found.");
        }

        if (notification.IsRead)
        {
            return Result.Success();
        }

        notification.MarkAsRead();
        await _repository.MarkAsReadAsync(notificationId, cancellationToken);

        return Result.Success();
    }
}
