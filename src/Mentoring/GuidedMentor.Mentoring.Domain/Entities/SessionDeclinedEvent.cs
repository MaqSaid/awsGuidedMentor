using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Domain event raised when a mentor declines a session request.
/// Triggers mentee notification and slot release.
/// </summary>
public sealed record SessionDeclinedEvent(
    SessionId SessionId,
    MenteeId MenteeId,
    MentorId MentorId,
    DateTime OccurredAt) : IDomainEvent;
