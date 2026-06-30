using FluentValidation;
using GuidedMentor.Mentoring.Domain.Entities;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Validates the CreateOpportunityCommand input fields.
/// </summary>
public sealed class CreateOpportunityValidator : AbstractValidator<CreateOpportunityCommand>
{
    public CreateOpportunityValidator()
    {
        RuleFor(x => x.MentorId)
            .NotEmpty().WithMessage("MentorId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(5, 100).WithMessage("Title must be between 5 and 100 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid opportunity type.");

        RuleFor(x => x.OrganisationName)
            .NotEmpty().WithMessage("Organisation name is required.")
            .Length(2, 100).WithMessage("Organisation name must be between 2 and 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .Length(100, 2000).WithMessage("Description must be between 100 and 2000 characters.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.");

        RuleFor(x => x.EventDateTime)
            .Must(dt => dt == null || dt > DateTime.UtcNow)
            .WithMessage("Event date must be in the future.")
            .When(x => x.Type is OpportunityType.Workshop or OpportunityType.Event or OpportunityType.Training);

        RuleFor(x => x.EmploymentType)
            .NotNull().WithMessage("Employment type is required for job postings.")
            .When(x => x.Type == OpportunityType.Job);

        RuleFor(x => x.EmploymentType)
            .Null().WithMessage("Employment type should not be set for non-job postings.")
            .When(x => x.Type != OpportunityType.Job);

        RuleFor(x => x.RequiredSkills)
            .Must(skills => skills.Count <= 10)
            .WithMessage("Maximum 10 required skills allowed.");

        RuleFor(x => x.RequiredExperience)
            .IsInEnum().WithMessage("Invalid experience level.");

        RuleFor(x => x.ExternalUrl)
            .NotEmpty().WithMessage("External URL is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https")
            .WithMessage("External URL must be a valid HTTPS URL.");
    }
}
