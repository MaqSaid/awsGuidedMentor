using FluentValidation;
using GuidedMentor.Identity.Application.DTOs.Onboarding;

namespace GuidedMentor.Identity.Application.Validators.Onboarding;

/// <summary>
/// Validates mentee onboarding Step 2 (Skills) data.
/// </summary>
public sealed class MenteeStep2Validator : AbstractValidator<MenteeStep2Data>
{
    private static readonly string[] ValidExperienceLevels = ["beginner", "intermediate", "advanced"];

    public MenteeStep2Validator()
    {
        RuleFor(x => x.Skills)
            .NotNull().WithMessage("Skills are required.")
            .Must(s => s is not null && s.Count >= 1)
            .WithMessage("At least 1 skill must be selected.")
            .Must(s => s is null || s.Count <= 10)
            .WithMessage("A maximum of 10 skills can be selected.");

        RuleFor(x => x.ExperienceLevel)
            .NotEmpty().WithMessage("Experience level is required.")
            .Must(level => ValidExperienceLevels.Contains(level.ToLowerInvariant()))
            .WithMessage("Experience level must be one of: beginner, intermediate, advanced.");

        RuleFor(x => x.YearsOfExperience)
            .InclusiveBetween(0, 50)
            .WithMessage("Years of experience must be between 0 and 50.");
    }
}
