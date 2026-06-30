namespace GuidedMentor.Identity.Domain.Entities;

/// <summary>
/// Strongly-typed identifier for the AdminUser entity.
/// </summary>
public sealed record AdminUserId(Guid Value)
{
    public static AdminUserId New() => new(Guid.NewGuid());
}
