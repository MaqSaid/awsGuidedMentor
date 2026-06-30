using FluentValidation;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Validates SetMaintenanceModeCommand: adminId required, reason 10-500 chars.
/// </summary>
public sealed class SetMaintenanceModeCommandValidator : AbstractValidator<SetMaintenanceModeCommand>
{
    public SetMaintenanceModeCommandValidator()
    {
        RuleFor(x => x.AdminId)
            .NotEmpty().WithMessage("Admin ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required for maintenance mode changes.")
            .MinimumLength(10).WithMessage("Reason must be at least 10 characters.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
