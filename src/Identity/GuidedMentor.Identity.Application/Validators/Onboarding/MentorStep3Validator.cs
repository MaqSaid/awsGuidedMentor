using FluentValidation;
using GuidedMentor.Identity.Application.DTOs.Onboarding;

namespace GuidedMentor.Identity.Application.Validators.Onboarding;

/// <summary>
/// Validates mentor onboarding Step 3 (Availability) data.
/// </summary>
public sealed class MentorStep3Validator : AbstractValidator<MentorStep3Data>
{
    private static readonly string[] ValidFormats = ["video_call", "voice_call", "chat"];

    public MentorStep3Validator()
    {
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
