namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Strongly-typed identifier for the Session entity.
/// </summary>
public sealed record SessionId(Guid Value)
{
    public static SessionId New() => new(Guid.NewGuid());
}
