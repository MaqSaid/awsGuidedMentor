using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Notifications;

/// <summary>
/// Handles batch marking all unread notifications for a user as read.
/// </summary>
public sealed class BatchMarkReadHandler : IRequestHandler<BatchMarkReadCommand, Result>
{
    private readonly INotificationRepository _repository;

    public BatchMarkReadHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(BatchMarkReadCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        await _repository.BatchMarkAsReadAsync(userId, cancellationToken);
        return Result.Success();
    }
}
