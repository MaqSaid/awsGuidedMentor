using GuidedMentor.Engagement.Application.DTOs;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Queries.Notifications;

/// <summary>
/// Retrieves the most recent notifications for a user (max 50, reverse chronological).
/// Unread notifications are distinguished by the IsRead property in the DTO.
/// </summary>
public sealed class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _repository;

    public GetNotificationsHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var limit = Math.Min(request.Limit, 50); // Cap at 50

        var notifications = await _repository.GetByRecipientAsync(userId, limit, cancellationToken);

        return notifications
            .Select(n => new NotificationDto(
                n.Id.Value,
                n.RecipientUserId.Value,
                n.Type,
                n.Message,
                n.ActionUrl,
                n.IsRead,
                n.CreatedAt))
            .ToList()
            .AsReadOnly();
    }
}
