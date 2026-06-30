using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Queries.Notifications;

/// <summary>
/// Returns the unread notification count for a user. Frontend uses this for badge display.
/// </summary>
public sealed class GetUnreadCountHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly INotificationRepository _repository;

    public GetUnreadCountHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        return await _repository.GetUnreadCountAsync(userId, cancellationToken);
    }
}
