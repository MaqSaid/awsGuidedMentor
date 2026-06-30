using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Domain.Events;

/// <summary>
/// Raised when a user's account is locked due to excessive failed login attempts.
/// </summary>
public sealed record AccountLockedEvent(
    UserId UserId,
    DateTime LockedUntil,
    DateTime OccurredAt) : IDomainEvent;
