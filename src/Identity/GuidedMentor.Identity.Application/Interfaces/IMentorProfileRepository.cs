using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.DTOs.Onboarding;

namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Repository for persisting completed mentor profiles to the Mentors_Table.
/// </summary>
public interface IMentorProfileRepository
{
    /// <summary>
    /// Persists a completed mentor profile assembled from all onboarding steps.
    /// </summary>
    Task SaveProfileAsync(
        Guid userId,
        MentorStep1Data profile,
        MentorStep2Data expertise,
        MentorStep3Data availability,
        CancellationToken ct);

    /// <summary>
    /// Gets the current active mentee count for a mentor.
    /// Used to enforce the constraint: new maxMentees ≥ activeMenteeCount.
    /// </summary>
    Task<int> GetActiveMenteeCountAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Updates the mentor profile with new settings data.
    /// </summary>
    Task UpdateProfileAsync(Guid userId, MentorSettingsData settings, CancellationToken ct);
}
