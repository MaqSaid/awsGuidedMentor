using FluentValidation;

namespace GuidedMentor.Mentoring.Application.Commands.Settings;

/// <summary>
/// Validates the UpdateSettingsCommand inputs using the same rules enforced during mentor onboarding.
/// Requirements: 13.2 — validate inputs against the same rules enforced during onboarding.
/// </summary>
public sealed class UpdateSettingsValidator : AbstractValidator<UpdateSettingsCommand>
{
    private static readonly string[] ValidSessionFormats = ["video_call", "voice_call", "chat"];

    public UpdateSettingsValidator()
    {
        RuleFor(x => x.MentorId)
            .NotEmpty().WithMessage("MentorId is required.");

        // Same as onboarding Step 1: full name 2-100 characters
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .Length(2, 100).WithMessage("Display name must be between 2 and 100 characters.");

        // Same as onboarding Step 1: professional title 2-100 characters
        RuleFor(x => x.ProfessionalTitle)
            .NotEmpty().WithMessage("Professional title is required.")
            .Length(2, 100).WithMessage("Professional title must be between 2 and 100 characters.");

        // Same as onboarding Step 1: company name 2-100 characters
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .Length(2, 100).WithMessage("Company name must be between 2 and 100 characters.");

        // Same as onboarding Step 1: chapter from predefined list (enum validation)
        RuleFor(x => x.Chapter)
            .IsInEnum().WithMessage("Invalid AWS User Group chapter.");

        // Same as onboarding Step 2: expertise areas 1-10 selections
        RuleFor(x => x.ExpertiseAreas)
            .NotNull().WithMessage("Expertise areas are required.")
            .Must(areas => areas.Count >= 1 && areas.Count <= 10)
            .WithMessage("Expertise areas must have between 1 and 10 selections.");

        // Same as onboarding Step 2: years of experience 1-30
        RuleFor(x => x.YearsOfExperience)
            .InclusiveBetween(1, 30).WithMessage("Years of experience must be between 1 and 30.");

        // Same as onboarding Step 2: certifications 0 or more (no max explicit in req, but reasonable)
        RuleFor(x => x.Certifications)
            .NotNull().WithMessage("Certifications list must not be null.")
            .Must(certs => certs.Count <= 15)
            .WithMessage("Maximum 15 certifications allowed.");

        // Same as onboarding Step 2: topics 1-10 selections
        RuleFor(x => x.Topics)
            .NotNull().WithMessage("Topics are required.")
            .Must(topics => topics.Count >= 1 && topics.Count <= 10)
            .WithMessage("Topics must have between 1 and 10 selections.");

        // Same as onboarding Step 3: maxMentees 1-5
        RuleFor(x => x.MaxMentees)
            .InclusiveBetween(1, 5).WithMessage("Maximum mentees must be between 1 and 5.");

        // Same as onboarding Step 3: session formats (one or more selections)
        RuleFor(x => x.SessionFormats)
            .NotNull().WithMessage("Session formats are required.")
            .Must(formats => formats.Count >= 1)
            .WithMessage("At least one session format must be selected.")
            .Must(formats => formats.All(f => ValidSessionFormats.Contains(f)))
            .WithMessage("Invalid session format. Must be 'video_call', 'voice_call', or 'chat'.");

        // Same as onboarding Step 3: bio 100-1000 characters
        RuleFor(x => x.Bio)
            .NotEmpty().WithMessage("Bio is required.")
            .Length(100, 1000).WithMessage("Bio must be between 100 and 1000 characters.");
    }
}
