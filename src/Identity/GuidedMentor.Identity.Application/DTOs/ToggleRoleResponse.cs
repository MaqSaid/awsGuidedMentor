using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.DTOs;

/// <summary>
/// Response returned after successfully toggling the active role.
/// </summary>
/// <param name="NewRole">The role that is now active.</param>
/// <param name="PreviousRole">The role that was previously active.</param>
/// <param name="RequiresOnboarding">Whether the user needs to complete onboarding for the new role.</param>
public sealed record ToggleRoleResponse(Role NewRole, Role PreviousRole, bool RequiresOnboarding);
