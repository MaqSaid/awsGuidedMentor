using GuidedMentor.Identity.Application.DTOs.Onboarding;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Onboarding;

/// <summary>
/// Handles retrieving onboarding progress. Loads all saved step data
/// and determines the current position in the wizard.
/// </summary>
public sealed class GetOnboardingProgressHandler
    : IRequestHandler<GetOnboardingProgressQuery, Result<OnboardingProgressResponse>>
{
    private readonly IOnboardingProgressRepository _progressRepository;

    public GetOnboardingProgressHandler(IOnboardingProgressRepository progressRepository)
    {
        _progressRepository = progressRepository;
    }

    public async Task<Result<OnboardingProgressResponse>> Handle(
        GetOnboardingProgressQuery request,
        CancellationToken cancellationToken)
    {
        var totalSteps = GetTotalSteps(request.Role);
        var savedData = await _progressRepository.GetProgressAsync(
            request.UserId, request.Role, cancellationToken);
        var lastCompletedStep = await _progressRepository.GetLastCompletedStepAsync(
            request.UserId, request.Role, cancellationToken);

        var currentStep = Math.Min(lastCompletedStep + 1, totalSteps);
        var isComplete = lastCompletedStep >= totalSteps;

        return Result<OnboardingProgressResponse>.Success(
            new OnboardingProgressResponse(
                CurrentStep: currentStep,
                TotalSteps: totalSteps,
                IsComplete: isComplete,
                SavedData: savedData));
    }

    private static int GetTotalSteps(Role role) => role switch
    {
        Role.Mentee => 4,
        Role.Mentor => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
