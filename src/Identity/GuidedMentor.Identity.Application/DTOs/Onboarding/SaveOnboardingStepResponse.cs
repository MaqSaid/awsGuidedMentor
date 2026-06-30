namespace GuidedMentor.Identity.Application.DTOs.Onboarding;

/// <summary>
/// Response returned after saving an onboarding step.
/// </summary>
public sealed record SaveOnboardingStepResponse(
    int CompletedStep,
    bool IsComplete,
    int TotalSteps);
