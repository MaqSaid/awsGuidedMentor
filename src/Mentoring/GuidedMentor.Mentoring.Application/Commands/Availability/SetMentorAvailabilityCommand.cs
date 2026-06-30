using GuidedMentor.Mentoring.Domain.ValueObjects;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Availability;

/// <summary>
/// Sets the mentor's availability status (available or unavailable).
/// When unavailable, the mentor is excluded from browse results but active sessions continue.
/// </summary>
public sealed record SetMentorAvailabilityCommand(
    Guid MentorId,
    AvailabilityStatus Status,
    UnavailabilityReason? Reason,
    DateTime? ReturnDate) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => MentorId;
    string IAuditableCommand.AuditResourceId => $"Mentor:{MentorId}:Availability:{Status}";
}
