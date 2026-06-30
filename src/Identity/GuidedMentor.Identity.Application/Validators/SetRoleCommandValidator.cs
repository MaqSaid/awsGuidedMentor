using FluentValidation;
using GuidedMentor.Identity.Application.Commands.RoleSelection;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.Validators;

/// <summary>
/// Validates the SetRoleCommand input.
/// UserId must not be empty and Role must be a valid enum value.
/// </summary>
public sealed class SetRoleCommandValidator : AbstractValidator<SetRoleCommand>
{
    public SetRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid value (Mentor or Mentee).");
    }
}
