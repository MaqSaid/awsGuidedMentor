using System.Text.RegularExpressions;

namespace GuidedMentor.Engagement.Application.Services;

/// <summary>
/// Sanitizes user-provided input before inclusion in AI prompts to prevent
/// prompt injection attacks. Strips control characters, neutralizes known
/// injection patterns, escapes template delimiters, and enforces max length.
/// 
/// The Help Assistant enforces a stricter 1000-character limit per message
/// (vs. 2000 for session plan fields in the Content context).
/// 
/// Validates: Requirements 14.9
/// </summary>
public static partial class InputSanitizer
{
    /// <summary>
    /// Maximum allowed characters per message for the AI Help Assistant.
    /// </summary>
    public const int MaxMessageLength = 1000;

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
        "from now on",
        "reveal your prompt",
        "show your instructions",
        "what are your instructions",
        "repeat your system",
        "print your prompt"
    ];

    /// <summary>
    /// Sanitizes a user message by applying all protection layers:
    /// 1. Strip control characters (preserving newline and tab)
    /// 2. Neutralize known injection patterns
    /// 3. Escape template delimiters
    /// 4. Enforce maximum length (1000 chars)
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
    /// Truncates input to the maximum allowed message length (1000 chars).
    /// </summary>
    internal static string EnforceMaxLength(string input)
    {
        if (input.Length <= MaxMessageLength)
            return input;

        return input[..MaxMessageLength];
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
