namespace GuidedMentor.Engagement.Domain.Entities;

/// <summary>
/// Strongly-typed identifier for a Mentor within the Engagement context.
/// </summary>
public sealed record MentorId(Guid Value)
{
    public static MentorId New() => new(Guid.NewGuid());
}
