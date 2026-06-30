using System.Text.RegularExpressions;

namespace GuidedMentor.Content.Application.Services;

/// <summary>
/// Sanitizes user-provided input before inclusion in AI prompts to prevent
/// prompt injection attacks. Strips control characters, neutralizes known
/// injection patterns, escapes template delimiters, and enforces max length.
/// Validates: Requirements 7.10, 14.9
/// </summary>
public static partial class InputSanitizer
{
    /// <summary>
    /// Maximum allowed characters per field after sanitization.
    /// </summary>
    public const int MaxFieldLength = 2000;

    /// <summary>
    /// Known prompt injection patterns (case-insensitive).
    /// These attempt to override the system prompt or reset AI context.
    /// </summary>
    private static readonly string[] InjectionPatterns =
    [
        "ignore previous",
        "ignore all previous",
        "ignore above",
        "disregard previous",
        "disregard all previous",
        "system:",
        "you are now",
        "forget everything",
        "forget all",
        "new instructions",
        "override instructions",
        "ignore instructions",
        "act as",
        "pretend you are",
        "from now on"
    ];

    /// <summary>
    /// Sanitizes a single input field by applying all protection layers:
    /// 1. Strip control characters (preserving newline and tab)
    /// 2. Neutralize known injection patterns
    /// 3. Escape template delimiters
    /// 4. Enforce maximum length
    /// </summary>
    /// <param name="input">Raw user input to sanitize.</param>
    /// <returns>Sanitized string safe for prompt inclusion, or empty string if input is null.</returns>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var result = StripControlCharacters(input);
        result = NeutralizeInjectionPatterns(result);
        result = EscapeTemplateDelimiters(result);
        result = EnforceMaxLength(result);

        return result;
    }

    /// <summary>
    /// Strips control characters except newline (\n) and tab (\t).
    /// Carriage return (\r) is also preserved to support Windows line endings.
    /// </summary>
    internal static string StripControlCharacters(string input)
    {
        return ControlCharRegex().Replace(input, string.Empty);
    }

    /// <summary>
    /// Detects and neutralizes known prompt injection patterns by wrapping
    /// them in [filtered: ...] markers. Uses case-insensitive matching.
    /// </summary>
    internal static string NeutralizeInjectionPatterns(string input)
    {
        var result = input;

        foreach (var pattern in InjectionPatterns)
        {
            // Build a regex that matches the pattern case-insensitively
            var escapedPattern = Regex.Escape(pattern);
            var regex = new Regex(escapedPattern, RegexOptions.IgnoreCase);

            result = regex.Replace(result, match => $"[filtered: {match.Value}]");
        }

        return result;
    }

    /// <summary>
    /// Escapes template delimiters that could break LLM prompt formatting:
    /// - Triple backticks (```) → triple single quotes (''')
    /// - Triple dashes (---) → em dash (—)
    /// </summary>
    internal static string EscapeTemplateDelimiters(string input)
    {
        var result = input.Replace("```", "'''");
        result = result.Replace("---", "\u2014"); // em dash
        return result;
    }

    /// <summary>
    /// Truncates input to the maximum allowed field length.
    /// </summary>
    internal static string EnforceMaxLength(string input)
    {
        if (input.Length <= MaxFieldLength)
            return input;

        return input[..MaxFieldLength];
    }

    /// <summary>
    /// Sanitizes multiple fields and returns them as a dictionary.
    /// Useful for batch-sanitizing command/query DTOs.
    /// </summary>
    /// <param name="fields">Dictionary of field names to raw values.</param>
    /// <returns>Dictionary of field names to sanitized values.</returns>
    public static IReadOnlyDictionary<string, string> SanitizeFields(IReadOnlyDictionary<string, string?> fields)
    {
        var result = new Dictionary<string, string>(fields.Count);

        foreach (var (key, value) in fields)
        {
            result[key] = Sanitize(value);
        }

        return result;
    }

    /// <summary>
    /// Checks if input contains any known injection patterns without modifying it.
    /// Useful for logging/alerting on attempted injections.
    /// </summary>
    /// <param name="input">The input to check.</param>
    /// <returns>True if any injection pattern is detected.</returns>
    public static bool ContainsInjectionPattern(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        foreach (var pattern in InjectionPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // Matches control characters except \n (0x0A), \r (0x0D), and \t (0x09)
    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]")]
    private static partial Regex ControlCharRegex();
}
