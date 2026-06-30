using System.Text.RegularExpressions;

namespace GuidedMentor.SharedKernel;

/// <summary>
/// Value object representing a validated email address.
/// </summary>
public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email>.Failure("Email cannot be empty.");

        var trimmed = email.Trim().ToLowerInvariant();

        if (trimmed.Length > 256)
            return Result<Email>.Failure("Email cannot exceed 256 characters.");

        if (!EmailRegex().IsMatch(trimmed))
            return Result<Email>.Failure("Email format is invalid.");

        return Result<Email>.Success(new Email(trimmed));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
