using FluentValidation;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Validates DisableUserCommand: adminId required, targetUserId required, reason 10-500 chars.
/// </summary>
public sealed class DisableUserCommandValidator : AbstractValidator<DisableUserCommand>
{
    public DisableUserCommandValidator()
    {
        RuleFor(x => x.AdminId)
            .NotEmpty().WithMessage("Admin ID is required.");

        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target user ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required for disabling a user account.")
            .MinimumLength(10).WithMessage("Reason must be at least 10 characters.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
