using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Domain.Events;

/// <summary>
/// Raised when a user changes their AWS User Group chapter.
/// The Mentoring context can handle this event to flag compatibility score recalculation.
/// </summary>
public sealed record ChapterChangedEvent(
    UserId UserId,
    AustralianChapter PreviousChapter,
    AustralianChapter NewChapter,
    DateTime OccurredAt) : IDomainEvent;
