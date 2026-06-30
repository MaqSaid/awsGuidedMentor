using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.ValueObjects;

/// <summary>
/// Represents a mentor's availability status for new mentorship sessions.
/// Unavailable mentors are excluded from browse results but retain their profile and active sessions.
/// </summary>
public sealed class MentorAvailability : ValueObject
{
    /// <summary>Whether the mentor is available or unavailable for new sessions.</summary>
    public AvailabilityStatus Status { get; }

    /// <summary>Optional reason for unavailability (vacation, personal commitment, workload, other).</summary>
    public UnavailabilityReason? Reason { get; }

    /// <summary>Optional expected return date set by the mentor.</summary>
    public DateTime? ReturnDate { get; }

    /// <summary>Timestamp when the mentor became unavailable. Null when available.</summary>
    public DateTime? UnavailableSince { get; }

    private MentorAvailability(
        AvailabilityStatus status,
        UnavailabilityReason? reason,
        DateTime? returnDate,
        DateTime? unavailableSince)
    {
        Status = status;
        Reason = reason;
        ReturnDate = returnDate;
        UnavailableSince = unavailableSince;
    }

    /// <summary>
    /// Whether this mentor should be excluded from browse results.
    /// </summary>
    public bool ShouldExcludeFromBrowse => Status == AvailabilityStatus.Unavailable;

    /// <summary>
    /// Whether a reminder should be sent (unavailable for more than 90 days).
    /// </summary>
    public bool ShouldSendReminder => Status == AvailabilityStatus.Unavailable
        && UnavailableSince.HasValue
        && (DateTime.UtcNow - UnavailableSince.Value).TotalDays > 90;

    /// <summary>
    /// Creates an Available status (default state for mentors).
    /// </summary>
    public static MentorAvailability Available()
    {
        return new MentorAvailability(AvailabilityStatus.Available, null, null, null);
    }

    /// <summary>
    /// Creates an Unavailable status with optional reason and return date.
    /// </summary>
    /// <param name="reason">Optional reason for being unavailable.</param>
    /// <param name="returnDate">Optional expected return date.</param>
    /// <param name="unavailableSince">When the mentor became unavailable (defaults to UtcNow).</param>
    public static MentorAvailability Unavailable(
        UnavailabilityReason? reason = null,
        DateTime? returnDate = null,
        DateTime? unavailableSince = null)
    {
        return new MentorAvailability(
            AvailabilityStatus.Unavailable,
            reason,
            returnDate,
            unavailableSince ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Restores from persisted data (e.g., DynamoDB read).
    /// </summary>
    public static MentorAvailability FromPersisted(
        AvailabilityStatus status,
        UnavailabilityReason? reason,
        DateTime? returnDate,
        DateTime? unavailableSince)
    {
        return new MentorAvailability(status, reason, returnDate, unavailableSince);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Status;
        yield return Reason;
        yield return ReturnDate;
        yield return UnavailableSince;
    }
}

/// <summary>
/// The availability status of a mentor.
/// </summary>
public enum AvailabilityStatus
{
    Available,
    Unavailable
}

/// <summary>
/// Reason why a mentor has set themselves as unavailable.
/// </summary>
public enum UnavailabilityReason
{
    Vacation,
    PersonalCommitment,
    Workload,
    Other
}
