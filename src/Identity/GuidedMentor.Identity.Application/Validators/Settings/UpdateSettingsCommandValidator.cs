using System.Text.Json;
using FluentValidation;
using GuidedMentor.Identity.Application.Commands.Settings;
using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.Validators.Settings;

/// <summary>
/// Validates the UpdateSettingsCommand. Deserializes the JSON data based on role
/// and delegates to the appropriate settings validator (same rules as onboarding).
/// </summary>
public sealed class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UpdateSettingsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid role (Mentor or Mentee).");

        RuleFor(x => x.Data)
            .NotNull().WithMessage("Settings data is required.");

        RuleFor(x => x)
            .Custom((command, context) =>
            {
                if (command.Data is null)
                    return;

                try
                {
                    var rawJson = command.Data.RootElement.GetRawText();

                    switch (command.Role)
                    {
                        case Role.Mentor:
                            var mentorData = JsonSerializer.Deserialize<MentorSettingsData>(rawJson, JsonOptions);
                            if (mentorData is null)
                            {
                                context.AddFailure("Data", "Invalid mentor settings data format.");
                                return;
                            }

                            var mentorValidator = new MentorSettingsValidator();
                            var mentorResult = mentorValidator.Validate(mentorData);
                            foreach (var error in mentorResult.Errors)
                            {
                                context.AddFailure(error.PropertyName, error.ErrorMessage);
                            }
                            break;

                        case Role.Mentee:
                            var menteeData = JsonSerializer.Deserialize<MenteeSettingsData>(rawJson, JsonOptions);
                            if (menteeData is null)
                            {
                                context.AddFailure("Data", "Invalid mentee settings data format.");
                                return;
                            }

                            var menteeValidator = new MenteeSettingsValidator();
                            var menteeResult = menteeValidator.Validate(menteeData);
                            foreach (var error in menteeResult.Errors)
                            {
                                context.AddFailure(error.PropertyName, error.ErrorMessage);
                            }
                            break;
                    }
                }
                catch (JsonException)
                {
                    context.AddFailure("Data", "Settings data is not valid JSON.");
                }
            });
    }
}
