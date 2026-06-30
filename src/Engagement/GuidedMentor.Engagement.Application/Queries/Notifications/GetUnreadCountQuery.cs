using MediatR;

namespace GuidedMentor.Engagement.Application.Queries.Notifications;

/// <summary>
/// Returns the count of unread notifications for badge display.
/// Badge shows exact count for 1-99, "99+" for counts exceeding 99, hidden when 0.
/// </summary>
public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<int>;
