using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.ValueObjects;

namespace GuidedMentor.Mentoring.Application.Interfaces;

/// <summary>
/// Repository interface for mentee data access within the Mentoring bounded context.
/// </summary>
public interface IMenteeRepository
{
    /// <summary>
    /// Gets mentee IDs that have active sessions with a given mentor.
    /// Used by the OpportunityPublished notification handler.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetMenteeIdsWithActiveSessionsForMentorAsync(
        Guid mentorId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets mentees who have opted into opportunity notifications and have at least N overlapping skills.
    /// Used for skill-matched notifications (≥2 skill overlap).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetSkillMatchedMenteeIdsAsync(
        IReadOnlyList<string> requiredSkills,
        int minimumOverlap,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the opportunity notification preferences for a specific mentee.
    /// </summary>
    Task<OpportunityNotificationPreferences?> GetOpportunityPreferencesAsync(
        Guid menteeId,
        CancellationToken ct = default);

    /// <summary>
    /// Saves the opportunity notification preferences for a mentee.
    /// </summary>
    Task SaveOpportunityPreferencesAsync(
        Guid menteeId,
        OpportunityNotificationPreferences preferences,
        CancellationToken ct = default);
}
