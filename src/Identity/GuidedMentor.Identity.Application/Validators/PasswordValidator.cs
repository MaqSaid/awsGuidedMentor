using System.Text.RegularExpressions;

namespace GuidedMentor.Identity.Application.Validators;

/// <summary>
/// Reusable password validation logic.
/// Requirements: minimum 12 characters, at least one uppercase, one lowercase, one digit, one special character.
/// </summary>
public static partial class PasswordValidator
{
    public const int MinLength = 12;
    public const string MinLengthMessage = "Password must be at least 12 characters long.";
    public const string UppercaseMessage = "Password must contain at least one uppercase letter.";
    public const string LowercaseMessage = "Password must contain at least one lowercase letter.";
    public const string DigitMessage = "Password must contain at least one digit.";
    public const string SpecialCharMessage = "Password must contain at least one special character.";

    public static IReadOnlyList<string> Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            errors.Add(MinLengthMessage);

        if (!UppercaseRegex().IsMatch(password ?? string.Empty))
            errors.Add(UppercaseMessage);

        if (!LowercaseRegex().IsMatch(password ?? string.Empty))
            errors.Add(LowercaseMessage);

        if (!DigitRegex().IsMatch(password ?? string.Empty))
            errors.Add(DigitMessage);

        if (!SpecialCharRegex().IsMatch(password ?? string.Empty))
            errors.Add(SpecialCharMessage);

        return errors;
    }

    public static bool IsValid(string password) => Validate(password).Count == 0;

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex SpecialCharRegex();
}
