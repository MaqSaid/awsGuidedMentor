using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Domain.Events;

/// <summary>
/// Raised when a mentor confirms attendance at a meetup event.
/// </summary>
public sealed record MeetupAttendanceConfirmedEvent(
    MeetupEventId MeetupEventId,
    MentorId MentorId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
