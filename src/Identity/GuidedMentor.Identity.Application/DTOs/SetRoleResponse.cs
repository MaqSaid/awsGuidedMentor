using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.DTOs;

/// <summary>
/// Response returned after successfully setting the initial role.
/// </summary>
/// <param name="Role">The role that was set.</param>
/// <param name="RequiresOnboarding">Whether the user needs to complete onboarding for this role.</param>
public sealed record SetRoleResponse(Role Role, bool RequiresOnboarding);
