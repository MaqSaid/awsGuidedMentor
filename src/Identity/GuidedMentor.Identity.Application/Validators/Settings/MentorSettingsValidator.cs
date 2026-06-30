using FluentValidation;
using GuidedMentor.Identity.Application.DTOs;

namespace GuidedMentor.Identity.Application.Validators.Settings;

/// <summary>
/// Validates mentor settings data using the same rules enforced during onboarding.
/// Combines rules from MentorStep1, MentorStep2, and MentorStep3 validators.
/// </summary>
public sealed class MentorSettingsValidator : AbstractValidator<MentorSettingsData>
{
    private static readonly string[] ValidFormats = ["video_call", "voice_call", "chat"];

    public MentorSettingsValidator()
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

        RuleFor(x => x.ProfessionalTitle)
            .NotEmpty().WithMessage("Professional title is required.")
            .MinimumLength(2).WithMessage("Professional title must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Professional title must not exceed 100 characters.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MinimumLength(2).WithMessage("Company name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Company name must not exceed 100 characters.");

        // Expertise fields (Step 2 rules)
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

        // Availability fields (Step 3 rules)
        RuleFor(x => x.MaxMentees)
            .InclusiveBetween(1, 5)
            .WithMessage("Maximum mentees must be between 1 and 5.");

        RuleFor(x => x.Availability)
            .NotNull().WithMessage("Availability is required.")
            .Must(a => a is not null && a.Count > 0)
            .WithMessage("At least one day of availability must be specified.")
            .Must(a => a is null || a.All(day => day.Value.Count > 0))
            .WithMessage("Each available day must have at least one time slot.");

        RuleFor(x => x.SessionFormats)
            .NotNull().WithMessage("Session formats are required.")
            .Must(f => f is not null && f.Count >= 1)
            .WithMessage("At least one session format must be selected.")
            .Must(f => f is null || f.All(format => ValidFormats.Contains(format.ToLowerInvariant())))
            .WithMessage("Session formats must be one of: video_call, voice_call, chat.");

        RuleFor(x => x.Bio)
            .NotEmpty().WithMessage("Bio is required.")
            .MinimumLength(100).WithMessage("Bio must be at least 100 characters.")
            .MaximumLength(1000).WithMessage("Bio must not exceed 1000 characters.");
    }
}
