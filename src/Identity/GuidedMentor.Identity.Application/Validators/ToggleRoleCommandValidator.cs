using FluentValidation;
using GuidedMentor.Identity.Application.Commands.RoleSelection;

namespace GuidedMentor.Identity.Application.Validators;

/// <summary>
/// Validates the ToggleRoleCommand input.
/// UserId must not be empty.
/// </summary>
public sealed class ToggleRoleCommandValidator : AbstractValidator<ToggleRoleCommand>
{
    public ToggleRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
