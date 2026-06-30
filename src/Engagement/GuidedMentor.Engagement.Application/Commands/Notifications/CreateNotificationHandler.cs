using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Notifications;

/// <summary>
/// Handles notification creation: validates, persists to DynamoDB, and pushes via AppSync.
/// </summary>
public sealed class CreateNotificationHandler : IRequestHandler<CreateNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly IAppSyncNotificationPublisher _appSyncPublisher;

    public CreateNotificationHandler(
        INotificationRepository repository,
        IAppSyncNotificationPublisher appSyncPublisher)
    {
        _repository = repository;
        _appSyncPublisher = appSyncPublisher;
    }

    public async Task<Result> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var recipientUserId = new UserId(request.RecipientUserId);

        var notificationResult = Notification.Create(
            recipientUserId,
            request.Type,
            request.Message,
            request.ActionUrl);

        if (notificationResult.IsFailure)
        {
            return Result.Failure(notificationResult.Error);
        }

        var notification = notificationResult.Value;

        // Persist to Notifications_Table
        await _repository.SaveAsync(notification, cancellationToken);

        // Push via AppSync subscription for real-time delivery (< 5 seconds)
        await _appSyncPublisher.PublishAsync(notification, cancellationToken);

        return Result.Success();
    }
}
