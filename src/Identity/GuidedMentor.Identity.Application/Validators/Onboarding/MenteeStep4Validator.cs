using FluentValidation;
using GuidedMentor.Identity.Application.DTOs.Onboarding;

namespace GuidedMentor.Identity.Application.Validators.Onboarding;

/// <summary>
/// Validates mentee onboarding Step 4 (Preferences) data.
/// </summary>
public sealed class MenteeStep4Validator : AbstractValidator<MenteeStep4Data>
{
    private static readonly string[] ValidCommunicationMethods = ["video_call", "voice_call", "chat"];

    public MenteeStep4Validator()
    {
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
