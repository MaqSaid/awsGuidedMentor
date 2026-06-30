using FluentValidation;
using GuidedMentor.Identity.Application.Validators;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// FluentValidation validator for EmailSignupCommand.
/// Validates email format and password complexity requirements.
/// </summary>
public sealed class EmailSignupValidator : AbstractValidator<EmailSignupCommand>
{
    public EmailSignupValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordValidator.MinLength)
                .WithMessage(PasswordValidator.MinLengthMessage)
            .Must(HaveUppercase)
                .WithMessage(PasswordValidator.UppercaseMessage)
            .Must(HaveLowercase)
                .WithMessage(PasswordValidator.LowercaseMessage)
            .Must(HaveDigit)
                .WithMessage(PasswordValidator.DigitMessage)
            .Must(HaveSpecialCharacter)
                .WithMessage(PasswordValidator.SpecialCharMessage);
    }

    private static bool HaveUppercase(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(char.IsUpper);

    private static bool HaveLowercase(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(char.IsLower);

    private static bool HaveDigit(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(char.IsDigit);

    private static bool HaveSpecialCharacter(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(c => !char.IsLetterOrDigit(c));
}
