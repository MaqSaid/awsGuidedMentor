using GuidedMentor.Mentoring.Domain.ValueObjects;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Represents a mentor within the Mentoring bounded context.
/// Manages availability status and capacity for new mentee sessions.
/// </summary>
public sealed class MentorEntity : Entity<Guid>
{
    /// <summary>The mentor's availability status for new sessions.</summary>
    public MentorAvailability Availability { get; private set; }

    /// <summary>Maximum number of active mentees the mentor accepts.</summary>
    public int MaxMentees { get; private set; }

    /// <summary>Current number of active mentees.</summary>
    public int ActiveMenteeCount { get; private set; }

    /// <summary>The mentor's display name.</summary>
    public string DisplayName { get; private set; }

    /// <summary>The mentor's AWS User Group chapter.</summary>
    public AustralianChapter? Chapter { get; private set; }

    /// <summary>
    /// Whether compatibility scores need recalculation (e.g., after chapter change).
    /// Consumed on the next browse request to recompute scores.
    /// </summary>
    public bool RequiresCompatibilityRecalculation { get; private set; }

    private MentorEntity(
        Guid id,
        string displayName,
        int maxMentees,
        int activeMenteeCount,
        MentorAvailability availability,
        AustralianChapter? chapter = null,
        bool requiresCompatibilityRecalculation = false)
    {
        Id = id;
        DisplayName = displayName;
        MaxMentees = maxMentees;
        ActiveMenteeCount = activeMenteeCount;
        Availability = availability;
        Chapter = chapter;
        RequiresCompatibilityRecalculation = requiresCompatibilityRecalculation;
    }

    /// <summary>
    /// Creates a new MentorEntity. Defaults to Available status.
    /// </summary>
    public static MentorEntity Create(
        Guid id,
        string displayName,
        int maxMentees,
        int activeMenteeCount,
        MentorAvailability? availability = null,
        AustralianChapter? chapter = null,
        bool requiresCompatibilityRecalculation = false)
    {
        return new MentorEntity(
            id,
            displayName,
            maxMentees,
            activeMenteeCount,
            availability ?? MentorAvailability.Available(),
            chapter,
            requiresCompatibilityRecalculation);
    }

    /// <summary>
    /// Sets the mentor as available for new sessions.
    /// Clears any unavailability reason and return date.
    /// </summary>
    public Result SetAvailable()
    {
        Availability = MentorAvailability.Available();
        return Result.Success();
    }

    /// <summary>
    /// Sets the mentor as unavailable for new sessions.
    /// Active sessions continue unaffected.
    /// </summary>
    /// <param name="reason">Optional reason for unavailability.</param>
    /// <param name="returnDate">Optional expected return date.</param>
    public Result SetUnavailable(UnavailabilityReason? reason = null, DateTime? returnDate = null)
    {
        Availability = MentorAvailability.Unavailable(reason, returnDate);
        return Result.Success();
    }

    /// <summary>
    /// Updates the maxMentees setting.
    /// Fails if the new value is less than the current active mentee count.
    /// </summary>
    /// <param name="newMaxMentees">The new maximum mentee count (1-5).</param>
    public Result UpdateMaxMentees(int newMaxMentees)
    {
        if (newMaxMentees < ActiveMenteeCount)
        {
            return Result.Failure(
                $"Cannot reduce maxMentees to {newMaxMentees}. " +
                $"You currently have {ActiveMenteeCount} active mentee(s). " +
                $"The value must be at least {ActiveMenteeCount}.");
        }

        MaxMentees = newMaxMentees;
        return Result.Success();
    }

    /// <summary>
    /// Updates the chapter and flags for compatibility score recalculation if changed.
    /// </summary>
    /// <param name="newChapter">The new AWS chapter.</param>
    public void UpdateChapter(AustralianChapter newChapter)
    {
        if (Chapter != newChapter)
        {
            Chapter = newChapter;
            RequiresCompatibilityRecalculation = true;
        }
    }

    /// <summary>
    /// Clears the compatibility recalculation flag after scores have been recomputed.
    /// </summary>
    public void ClearRecalculationFlag()
    {
        RequiresCompatibilityRecalculation = false;
    }

    /// <summary>
    /// Whether this mentor should appear in browse results.
    /// Excluded when unavailable or at full capacity.
    /// </summary>
    public bool IsVisibleInBrowse =>
        !Availability.ShouldExcludeFromBrowse && ActiveMenteeCount < MaxMentees;
}
