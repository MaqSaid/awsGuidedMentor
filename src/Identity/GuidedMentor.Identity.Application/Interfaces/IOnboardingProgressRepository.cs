using System.Text.Json;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Repository for persisting and loading onboarding progress per user/role.
/// Progress is stored separately from the final profile (DynamoDB progress records).
/// </summary>
public interface IOnboardingProgressRepository
{
    /// <summary>
    /// Saves step data for a specific user, role, and step number.
    /// </summary>
    Task SaveStepAsync(Guid userId, Role role, int step, JsonDocument data, CancellationToken ct);

    /// <summary>
    /// Loads all saved step data for a user and role.
    /// Returns a dictionary of step number to step data.
    /// </summary>
    Task<Dictionary<int, JsonDocument>> GetProgressAsync(Guid userId, Role role, CancellationToken ct);

    /// <summary>
    /// Gets the highest completed step for a user and role.
    /// Returns 0 if no steps have been completed.
    /// </summary>
    Task<int> GetLastCompletedStepAsync(Guid userId, Role role, CancellationToken ct);
}
