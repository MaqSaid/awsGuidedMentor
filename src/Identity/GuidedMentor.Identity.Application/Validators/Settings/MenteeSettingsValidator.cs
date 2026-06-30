using FluentValidation;
using GuidedMentor.Identity.Application.DTOs;

namespace GuidedMentor.Identity.Application.Validators.Settings;

/// <summary>
/// Validates mentee settings data using the same rules enforced during onboarding.
/// Combines rules from MenteeStep1, MenteeStep2, MenteeStep3, and MenteeStep4 validators.
/// </summary>
public sealed class MenteeSettingsValidator : AbstractValidator<MenteeSettingsData>
{
    private static readonly string[] ValidExperienceLevels = ["beginner", "intermediate", "advanced"];
    private static readonly string[] ValidGoals =
        ["career_transition", "skill_development", "certification_preparation", "project_guidance"];
    private static readonly string[] ValidDurations = ["4_weeks", "8_weeks", "12_weeks"];
    private static readonly string[] ValidCommunicationMethods = ["video_call", "voice_call", "chat"];

    public MenteeSettingsValidator()
    {
        // Profile fields (Step 1 rules)
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.ProfilePhotoUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Profile photo URL must be a valid URL.");

        RuleFor(x => x.AwsChapter)
            .IsInEnum().WithMessage("AWS chapter must be a valid Australian chapter.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.");

        // Skills fields (Step 2 rules)
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

        // Goals fields (Step 3 rules)
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

        // Preferences fields (Step 4 rules)
        RuleFor(x => x.Availability)
            .NotNull().WithMessage("Availability is required.")
            .Must(a => a is not null && a.Count > 0)
            .WithMessage("At least one day of availability must be specified.")
            .Must(a => a is null || a.All(day => day.Value.Count > 0))
            .WithMessage("Each available day must have at least one time slot.");

        RuleFor(x => x.CommunicationPreference)
            .NotEmpty().WithMessage("Communication preference is required.")
            .Must(c => ValidCommunicationMethods.Contains(c.ToLowerInvariant()))
            .WithMessage("Communication preference must be one of: video_call, voice_call, chat.");

        RuleFor(x => x.ResumeUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Resume URL must be a valid URL.");
    }
}
