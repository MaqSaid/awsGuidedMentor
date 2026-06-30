using FluentValidation;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Validates CreateMeetupEventCommand input fields.
/// </summary>
public sealed class CreateMeetupEventValidator : AbstractValidator<CreateMeetupEventCommand>
{
    public CreateMeetupEventValidator()
    {
        RuleFor(x => x.ChapterLeadId)
            .NotEmpty().WithMessage("ChapterLeadId is required.");

        RuleFor(x => x.Chapter)
            .IsInEnum().WithMessage("Invalid Australian chapter.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(5, 200).WithMessage("Title must be between 5 and 200 characters.");

        RuleFor(x => x.EventDate)
            .Must(d => d.Date > DateTime.UtcNow.Date)
            .WithMessage("Event date must be in the future.");

        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("Start time must be before end time.");

        RuleFor(x => x.VenueName)
            .NotEmpty().WithMessage("Venue name is required.")
            .Length(2, 200).WithMessage("Venue name must be between 2 and 200 characters.");

        RuleFor(x => x.VenueAddress)
            .NotEmpty().WithMessage("Venue address is required.")
            .Length(5, 500).WithMessage("Venue address must be between 5 and 500 characters.");

        RuleFor(x => x.EventUrl)
            .NotEmpty().WithMessage("Event URL is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https")
            .WithMessage("Event URL must be a valid HTTPS URL.");
    }
}
