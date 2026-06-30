namespace GuidedMentor.Engagement.Domain.Entities;

/// <summary>
/// Strongly-typed identifier for the MeetupEvent aggregate.
/// </summary>
public sealed record MeetupEventId(Guid Value)
{
    public static MeetupEventId New() => new(Guid.NewGuid());
}
