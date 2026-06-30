using System.Text.Json;

namespace GuidedMentor.Identity.Application.DTOs.Onboarding;

/// <summary>
/// Response containing the current onboarding progress for a user/role.
/// </summary>
public sealed record OnboardingProgressResponse(
    int CurrentStep,
    int TotalSteps,
    bool IsComplete,
    Dictionary<int, JsonDocument> SavedData);
