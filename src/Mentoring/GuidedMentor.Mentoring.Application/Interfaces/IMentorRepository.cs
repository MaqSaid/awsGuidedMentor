using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.ValueObjects;

namespace GuidedMentor.Mentoring.Application.Interfaces;

/// <summary>
/// Repository interface for mentor data access within the Mentoring bounded context.
/// Browse queries should filter out unavailable mentors (availabilityStatus = 'unavailable').
/// </summary>
public interface IMentorRepository
{
    /// <summary>
    /// Gets a mentor entity by its ID.
    /// </summary>
    Task<MentorEntity?> GetByIdAsync(Guid mentorId, CancellationToken ct = default);

    /// <summary>
    /// Persists the mentor's availability status to the Mentors_Table.
    /// Updates availabilityStatus, unavailabilityReason, returnDate, and unavailableSince attributes.
    /// </summary>
    Task SaveAvailabilityAsync(MentorEntity mentor, CancellationToken ct = default);

    /// <summary>
    /// Gets the current availability status for a mentor.
    /// </summary>
    Task<MentorAvailability?> GetAvailabilityAsync(Guid mentorId, CancellationToken ct = default);

    /// <summary>
    /// Returns all mentors who have been unavailable for more than the specified number of days.
    /// Used by the daily EventBridge job to send 90-day reminders.
    /// </summary>
    Task<IReadOnlyList<MentorEntity>> GetUnavailableMentorsOverDaysAsync(
        int days,
        CancellationToken ct = default);

    /// <summary>
    /// Increments the activeMenteeCount for the specified mentor.
    /// Used when a mentor accepts a session request.
    /// </summary>
    Task IncrementActiveMenteeCountAsync(Guid mentorId, CancellationToken ct = default);

    /// <summary>
    /// Decrements the activeMenteeCount for the specified mentor.
    /// Used when a session reaches Completed status.
    /// Ensures the count does not go below zero.
    /// </summary>
    Task DecrementActiveMenteeCountAsync(Guid mentorId, CancellationToken ct = default);

    /// <summary>
    /// Persists updated mentor settings (profile fields, maxMentees, chapter, etc.) to the Mentors_Table.
    /// Also saves the requiresCompatibilityRecalculation flag.
    /// </summary>
    Task SaveSettingsAsync(MentorEntity mentor, CancellationToken ct = default);
}
