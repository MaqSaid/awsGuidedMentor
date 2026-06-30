using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Domain.Events;

/// <summary>
/// Raised when a meetup event is cancelled by a chapter lead.
/// Used to notify affected mentor-mentee pairs with aligned sessions.
/// </summary>
public sealed record MeetupEventCancelledEvent(
    MeetupEventId MeetupEventId,
    AustralianChapter Chapter,
    string Title,
    DateTime EventDate) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
