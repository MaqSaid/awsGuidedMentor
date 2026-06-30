using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Domain event raised when a mentor accepts a session request.
/// Triggers plan generation and mentor capacity increment.
/// </summary>
public sealed record SessionAcceptedEvent(
    SessionId SessionId,
    MenteeId MenteeId,
    MentorId MentorId,
    DateTime OccurredAt) : IDomainEvent;
