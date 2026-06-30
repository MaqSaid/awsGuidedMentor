namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Strongly-typed identifier for the Mentee entity.
/// </summary>
public sealed record MenteeId(Guid Value)
{
    public static MenteeId New() => new(Guid.NewGuid());
}
