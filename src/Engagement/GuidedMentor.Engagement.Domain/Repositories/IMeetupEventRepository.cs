using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Domain.Repositories;

/// <summary>
/// Repository interface for MeetupEvent persistence (DynamoDB Meetups_Table).
/// Uses GSI-Chapter (PK=chapter, SK=eventDate) for chapter-based queries.
/// </summary>
public interface IMeetupEventRepository
{
    /// <summary>
    /// Persists a new or updated meetup event.
    /// </summary>
    Task SaveAsync(MeetupEvent meetupEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a meetup event by its unique identifier.
    /// </summary>
    Task<MeetupEvent?> GetByIdAsync(MeetupEventId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets upcoming (non-cancelled, future) meetup events for a chapter,
    /// sorted by eventDate ascending, limited to the specified count.
    /// Uses GSI-Chapter (PK=chapter, SK=eventDate).
    /// </summary>
    Task<IReadOnlyList<MeetupEvent>> GetUpcomingByChapterAsync(
        AustralianChapter chapter,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all session IDs aligned to a specific meetup event.
    /// Used when cancelling a meetup to notify affected pairs.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAlignedSessionIdsAsync(
        MeetupEventId meetupEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Associates a session with a meetup event (session date matches meetup date).
    /// </summary>
    Task AlignSessionAsync(
        Guid sessionId,
        MeetupEventId meetupEventId,
        CancellationToken cancellationToken = default);
}
