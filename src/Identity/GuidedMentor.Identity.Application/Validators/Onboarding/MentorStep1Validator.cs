using FluentValidation;
using GuidedMentor.Identity.Application.DTOs.Onboarding;

namespace GuidedMentor.Identity.Application.Validators.Onboarding;

/// <summary>
/// Validates mentor onboarding Step 1 (Profile) data.
/// </summary>
public sealed class MentorStep1Validator : AbstractValidator<MentorStep1Data>
{
    public MentorStep1Validator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.ProfilePhotoUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Profile photo URL must be a valid URL.");

        RuleFor(x => x.AwsChapter)
            .IsInEnum().WithMessage("AWS chapter must be a valid Australian chapter.");

        RuleFor(x => x.ProfessionalTitle)
            .NotEmpty().WithMessage("Professional title is required.")
            .MinimumLength(2).WithMessage("Professional title must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Professional title must not exceed 100 characters.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MinimumLength(2).WithMessage("Company name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Company name must not exceed 100 characters.");
    }
}
