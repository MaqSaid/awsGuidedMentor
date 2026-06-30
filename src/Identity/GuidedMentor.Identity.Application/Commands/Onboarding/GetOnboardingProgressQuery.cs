using GuidedMentor.Identity.Application.DTOs.Onboarding;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Onboarding;

/// <summary>
/// Retrieves current onboarding progress for a user and role.
/// Returns the current step, total steps, completion state, and all saved data.
/// </summary>
public sealed record GetOnboardingProgressQuery(
    Guid UserId,
    Role Role) : IRequest<Result<OnboardingProgressResponse>>;
