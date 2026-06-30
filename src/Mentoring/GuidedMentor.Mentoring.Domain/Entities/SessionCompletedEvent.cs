using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Domain event raised when both parties confirm session completion.
/// Triggers mentor capacity decrement and EventBridge session-completed event.
/// </summary>
public sealed record SessionCompletedEvent(
    SessionId SessionId,
    MenteeId MenteeId,
    MentorId MentorId,
    DateTime OccurredAt) : IDomainEvent;
