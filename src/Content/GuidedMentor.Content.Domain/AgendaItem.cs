namespace GuidedMentor.Content.Domain;

/// <summary>
/// Represents a single timed item within a session plan agenda.
/// Each item has a title, duration in minutes, and a description.
/// </summary>
public sealed record AgendaItem
{
    /// <summary>Title of the agenda item.</summary>
    public string Title { get; init; }

    /// <summary>Duration in minutes allocated to this agenda item. Must be at least 3 minutes.</summary>
    public int DurationMinutes { get; init; }

    /// <summary>Description of what this agenda item covers. Maximum 500 characters.</summary>
    public string Description { get; init; }

    public AgendaItem(string title, int durationMinutes, string description)
    {
        Title = title;
        DurationMinutes = durationMinutes;
        Description = description;
    }

    /// <summary>
    /// Validates that this agenda item meets all constraints:
    /// - Title is not empty
    /// - DurationMinutes is at least 3
    /// - Description is not empty and at most 500 characters
    /// </summary>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Title)
        && DurationMinutes >= 3
        && !string.IsNullOrWhiteSpace(Description)
        && Description.Length <= 500;
}
