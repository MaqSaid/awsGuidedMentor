using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.DTOs.Onboarding;

namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Repository for persisting completed mentee profiles to the Mentees_Table.
/// </summary>
public interface IMenteeProfileRepository
{
    /// <summary>
    /// Persists a completed mentee profile assembled from all onboarding steps.
    /// </summary>
    Task SaveProfileAsync(
        Guid userId,
        MenteeStep1Data profile,
        MenteeStep2Data skills,
        MenteeStep3Data goals,
        MenteeStep4Data preferences,
        CancellationToken ct);

    /// <summary>
    /// Updates the mentee profile with new settings data.
    /// </summary>
    Task UpdateProfileAsync(Guid userId, MenteeSettingsData settings, CancellationToken ct);
}
