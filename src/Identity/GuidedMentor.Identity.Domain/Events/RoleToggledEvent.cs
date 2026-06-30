using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Domain.Events;

/// <summary>
/// Raised when a user toggles their active role.
/// </summary>
public sealed record RoleToggledEvent(
    UserId UserId,
    Role PreviousRole,
    Role NewRole,
    DateTime OccurredAt) : IDomainEvent;
