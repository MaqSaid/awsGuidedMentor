namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Strongly-typed identifier for the MentorLock entity.
/// </summary>
public sealed record LockId(Guid Value)
{
    public static LockId New() => new(Guid.NewGuid());
}
