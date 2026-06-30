using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.RoleSelection;

/// <summary>
/// Handles toggling the user's active role.
/// On persistence failure, the in-memory state is discarded (DB remains unchanged).
/// </summary>
public sealed class ToggleRoleHandler : IRequestHandler<ToggleRoleCommand, Result<ToggleRoleResponse>>
{
    private readonly IUserRepository _userRepository;

    public ToggleRoleHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<ToggleRoleResponse>> Handle(
        ToggleRoleCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result<ToggleRoleResponse>.Failure("User not found.");
        }

        // Capture previous role before toggling
        var previousRole = user.ActiveRole;

        if (previousRole is null)
        {
            return Result<ToggleRoleResponse>.Failure(
                "Cannot toggle role. No active role has been set.");
        }

        var toggleResult = user.ToggleRole();

        if (toggleResult.IsFailure)
        {
            return Result<ToggleRoleResponse>.Failure(toggleResult.Error);
        }

        // Persist — if this throws, the in-memory change is lost (no DB update occurred)
        await _userRepository.SaveAsync(user, cancellationToken);

        var newRole = user.ActiveRole!.Value;
        var requiresOnboarding = user.GetOnboardingStatus(newRole) != OnboardingStatus.Completed;

        return Result<ToggleRoleResponse>.Success(
            new ToggleRoleResponse(newRole, previousRole.Value, requiresOnboarding));
    }
}
