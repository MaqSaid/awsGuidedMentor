using FluentValidation;
using GuidedMentor.Identity.Application.DTOs.Onboarding;

namespace GuidedMentor.Identity.Application.Validators.Onboarding;

/// <summary>
/// Validates mentor onboarding Step 2 (Expertise) data.
/// </summary>
public sealed class MentorStep2Validator : AbstractValidator<MentorStep2Data>
{
    public MentorStep2Validator()
    {
        RuleFor(x => x.ExpertiseAreas)
            .NotNull().WithMessage("Expertise areas are required.")
            .Must(e => e is not null && e.Count >= 1)
            .WithMessage("At least 1 expertise area must be selected.")
            .Must(e => e is null || e.Count <= 10)
            .WithMessage("A maximum of 10 expertise areas can be selected.");

        RuleFor(x => x.YearsOfExperience)
            .InclusiveBetween(1, 30)
            .WithMessage("Years of experience must be between 1 and 30.");

        RuleFor(x => x.Certifications)
            .NotNull().WithMessage("Certifications list is required (can be empty).");

        RuleFor(x => x.Topics)
            .NotNull().WithMessage("Topics are required.")
            .Must(t => t is not null && t.Count >= 1)
            .WithMessage("At least 1 topic must be selected.")
            .Must(t => t is null || t.Count <= 10)
            .WithMessage("A maximum of 10 topics can be selected.");
    }
}
