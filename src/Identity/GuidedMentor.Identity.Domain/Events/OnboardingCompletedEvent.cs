using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Domain.Events;

/// <summary>
/// Raised when a user completes onboarding for a specific role.
/// </summary>
public sealed record OnboardingCompletedEvent(
    UserId UserId,
    Role CompletedRole,
    DateTime OccurredAt) : IDomainEvent;
