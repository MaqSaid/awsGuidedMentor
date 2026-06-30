using System.Text.Json;
using FluentValidation;
using GuidedMentor.Identity.Application.DTOs.Onboarding;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Onboarding;

/// <summary>
/// Handles saving an onboarding step. Validates step data, persists progress,
/// and on final step completion persists the full profile and marks onboarding complete.
/// </summary>
public sealed class SaveOnboardingStepHandler
    : IRequestHandler<SaveOnboardingStepCommand, Result<SaveOnboardingStepResponse>>
{
    private readonly IOnboardingProgressRepository _progressRepository;
    private readonly IMenteeProfileRepository _menteeProfileRepository;
    private readonly IMentorProfileRepository _mentorProfileRepository;
    private readonly GuidedMentor.Identity.Domain.Repositories.IUserRepository _userRepository;

    public SaveOnboardingStepHandler(
        IOnboardingProgressRepository progressRepository,
        IMenteeProfileRepository menteeProfileRepository,
        IMentorProfileRepository mentorProfileRepository,
        GuidedMentor.Identity.Domain.Repositories.IUserRepository userRepository)
    {
        _progressRepository = progressRepository;
        _menteeProfileRepository = menteeProfileRepository;
        _mentorProfileRepository = mentorProfileRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<SaveOnboardingStepResponse>> Handle(
        SaveOnboardingStepCommand request,
        CancellationToken cancellationToken)
    {
        var totalSteps = GetTotalSteps(request.Role);

        if (request.Step < 1 || request.Step > totalSteps)
        {
            return Result<SaveOnboardingStepResponse>.Failure(
                $"Step must be between 1 and {totalSteps} for role {request.Role}.");
        }

        // Save step progress
        await _progressRepository.SaveStepAsync(
            request.UserId, request.Role, request.Step, request.Data, cancellationToken);

        var isFinalStep = request.Step == totalSteps;

        if (isFinalStep)
        {
            // Check all steps are complete
            var savedData = await _progressRepository.GetProgressAsync(
                request.UserId, request.Role, cancellationToken);

            // Include the current step data in the saved data for final validation
            savedData[request.Step] = request.Data;

            if (savedData.Count == totalSteps)
            {
                // All steps complete — persist the full profile and update user onboarding status
                await PersistCompleteProfileAsync(
                    request.UserId, request.Role, savedData, cancellationToken);

                // Update onboardingStatus to Completed on the User aggregate
                var user = await _userRepository.GetByIdAsync(
                    new UserId(request.UserId), cancellationToken);

                if (user is not null)
                {
                    user.SetOnboardingStatus(request.Role, OnboardingStatus.Completed);
                    await _userRepository.SaveAsync(user, cancellationToken);
                }
            }
        }
        else
        {
            // Mark onboarding as InProgress if not already
            var user = await _userRepository.GetByIdAsync(
                new UserId(request.UserId), cancellationToken);

            if (user is not null && user.GetOnboardingStatus(request.Role) == OnboardingStatus.NotStarted)
            {
                user.SetOnboardingStatus(request.Role, OnboardingStatus.InProgress);
                await _userRepository.SaveAsync(user, cancellationToken);
            }
        }

        return Result<SaveOnboardingStepResponse>.Success(
            new SaveOnboardingStepResponse(
                CompletedStep: request.Step,
                IsComplete: isFinalStep,
                TotalSteps: totalSteps));
    }

    private async Task PersistCompleteProfileAsync(
        Guid userId,
        Role role,
        Dictionary<int, JsonDocument> savedData,
        CancellationToken ct)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        switch (role)
        {
            case Role.Mentee:
                var menteeStep1 = JsonSerializer.Deserialize<MenteeStep1Data>(
                    savedData[1].RootElement.GetRawText(), options)!;
                var menteeStep2 = JsonSerializer.Deserialize<MenteeStep2Data>(
                    savedData[2].RootElement.GetRawText(), options)!;
                var menteeStep3 = JsonSerializer.Deserialize<MenteeStep3Data>(
                    savedData[3].RootElement.GetRawText(), options)!;
                var menteeStep4 = JsonSerializer.Deserialize<MenteeStep4Data>(
                    savedData[4].RootElement.GetRawText(), options)!;

                await _menteeProfileRepository.SaveProfileAsync(
                    userId, menteeStep1, menteeStep2, menteeStep3, menteeStep4, ct);
                break;

            case Role.Mentor:
                var mentorStep1 = JsonSerializer.Deserialize<MentorStep1Data>(
                    savedData[1].RootElement.GetRawText(), options)!;
                var mentorStep2 = JsonSerializer.Deserialize<MentorStep2Data>(
                    savedData[2].RootElement.GetRawText(), options)!;
                var mentorStep3 = JsonSerializer.Deserialize<MentorStep3Data>(
                    savedData[3].RootElement.GetRawText(), options)!;

                await _mentorProfileRepository.SaveProfileAsync(
                    userId, mentorStep1, mentorStep2, mentorStep3, ct);
                break;
        }
    }

    private static int GetTotalSteps(Role role) => role switch
    {
        Role.Mentee => 4,
        Role.Mentor => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
