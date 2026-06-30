using FluentValidation;
using GuidedMentor.Identity.Application.Commands.Onboarding;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.Validators.Onboarding;

/// <summary>
/// FluentValidation validator for SaveOnboardingStepCommand.
/// Validates the command envelope (userId, role, step range).
/// Step-level data validation is handled by the individual step validators
/// invoked within the handler based on role and step number.
/// </summary>
public sealed class SaveOnboardingStepValidator : AbstractValidator<SaveOnboardingStepCommand>
{
    public SaveOnboardingStepValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be Mentor or Mentee.");

        RuleFor(x => x.Step)
            .GreaterThanOrEqualTo(1).WithMessage("Step must be at least 1.")
            .Must((command, step) => step <= GetMaxStep(command.Role))
            .WithMessage("Step exceeds the maximum for the specified role.");

        RuleFor(x => x.Data)
            .NotNull().WithMessage("Step data is required.");
    }

    private static int GetMaxStep(Role role) => role switch
    {
        Role.Mentee => 4,
        Role.Mentor => 3,
        _ => 0
    };
}
