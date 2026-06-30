namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Strongly-typed identifier for the Mentor entity.
/// </summary>
public sealed record MentorId(Guid Value)
{
    public static MentorId New() => new(Guid.NewGuid());
}
