using FluentValidation;
using GuidedMentor.Identity.Application.DTOs.Onboarding;

namespace GuidedMentor.Identity.Application.Validators.Onboarding;

/// <summary>
/// Validates mentee onboarding Step 3 (Goals) data.
/// </summary>
public sealed class MenteeStep3Validator : AbstractValidator<MenteeStep3Data>
{
    private static readonly string[] ValidGoals =
        ["career_transition", "skill_development", "certification_preparation", "project_guidance"];

    private static readonly string[] ValidDurations = ["4_weeks", "8_weeks", "12_weeks"];

    public MenteeStep3Validator()
    {
        RuleFor(x => x.PrimaryGoal)
            .NotEmpty().WithMessage("Primary goal is required.")
            .Must(goal => ValidGoals.Contains(goal.ToLowerInvariant()))
            .WithMessage("Primary goal must be one of: career_transition, skill_development, certification_preparation, project_guidance.");

        RuleFor(x => x.GoalDescription)
            .NotEmpty().WithMessage("Goal description is required.")
            .MinimumLength(50).WithMessage("Goal description must be at least 50 characters.")
            .MaximumLength(500).WithMessage("Goal description must not exceed 500 characters.");

        RuleFor(x => x.PreferredDuration)
            .NotEmpty().WithMessage("Preferred duration is required.")
            .Must(d => ValidDurations.Contains(d.ToLowerInvariant()))
            .WithMessage("Preferred duration must be one of: 4_weeks, 8_weeks, 12_weeks.");
    }
}
