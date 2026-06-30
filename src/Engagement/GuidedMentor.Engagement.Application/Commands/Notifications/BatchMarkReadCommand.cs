using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Notifications;

/// <summary>
/// Marks all unread notifications for a user as read (bulk clear).
/// </summary>
public sealed record BatchMarkReadCommand(Guid UserId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => UserId;
    string IAuditableCommand.AuditResourceId => $"Notifications:User:{UserId}:BatchRead";
}
