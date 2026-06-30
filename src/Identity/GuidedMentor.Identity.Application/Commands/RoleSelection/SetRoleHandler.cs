using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.RoleSelection;

/// <summary>
/// Handles setting the initial role for a user.
/// Persists the role to the Users_Table and determines if onboarding is required.
/// </summary>
public sealed class SetRoleHandler : IRequestHandler<SetRoleCommand, Result<SetRoleResponse>>
{
    private readonly IUserRepository _userRepository;

    public SetRoleHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<SetRoleResponse>> Handle(
        SetRoleCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result<SetRoleResponse>.Failure("User not found.");
        }

        var result = user.SetInitialRole(request.Role);

        if (result.IsFailure)
        {
            return Result<SetRoleResponse>.Failure(result.Error);
        }

        await _userRepository.SaveAsync(user, cancellationToken);

        var requiresOnboarding = user.GetOnboardingStatus(request.Role) != OnboardingStatus.Completed;

        return Result<SetRoleResponse>.Success(
            new SetRoleResponse(request.Role, requiresOnboarding));
    }
}
