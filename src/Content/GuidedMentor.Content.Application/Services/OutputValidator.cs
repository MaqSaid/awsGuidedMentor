using System.Text.RegularExpressions;
using GuidedMentor.Content.Domain;

namespace GuidedMentor.Content.Application.Services;

/// <summary>
/// Validates AI-generated output before persisting or displaying to users.
/// Ensures no PII is present, no harmful content is included, and the response
/// conforms strictly to the expected JSON schema (SessionPlan domain model).
/// 
/// This works alongside InputSanitizer (which handles input safety) — this service
/// handles output safety as the final application-level gate before persistence.
/// 
/// Validates: Requirements 7.11, 7.12, 21.17
/// </summary>
public sealed partial class OutputValidator
{
    /// <summary>
    /// Result of output validation containing pass/fail status and any violation details.
    /// </summary>
    public sealed record ValidationResult
    {
        public bool IsValid { get; init; }
        public IReadOnlyList<string> Violations { get; init; } = [];

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Failure(IReadOnlyList<string> violations) =>
            new() { IsValid = false, Violations = violations };
    }

    /// <summary>
    /// Validates a generated SessionPlan for PII, harmful content, and schema conformance.
    /// All three checks must pass for the plan to be persisted.
    /// </summary>
    /// <param name="sessionPlan">The AI-generated session plan to validate.</param>
    /// <returns>A ValidationResult indicating whether the plan is safe to persist.</returns>
    public ValidationResult Validate(SessionPlan sessionPlan)
    {
        var violations = new List<string>();

        ValidateSchemaConformance(sessionPlan, violations);
        ValidateNoPiiPresent(sessionPlan, violations);
        ValidateNoHarmfulContent(sessionPlan, violations);

        return violations.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(violations);
    }

    /// <summary>
    /// Validates that the session plan conforms to the expected schema constraints.
    /// Delegates to the domain model's own validation plus additional output-specific checks.
    /// </summary>
    private static void ValidateSchemaConformance(SessionPlan plan, List<string> violations)
    {
        if (!plan.IsValid())
        {
            // Provide specific schema violations for diagnostics
            if (string.IsNullOrWhiteSpace(plan.SessionTitle))
                violations.Add("SessionTitle is empty.");
            else if (plan.SessionTitle.Length > 100)
                violations.Add($"SessionTitle exceeds 100 characters (actual: {plan.SessionTitle.Length}).");

            if (plan.Agenda.Count < 3 || plan.Agenda.Count > 7)
                violations.Add($"Agenda must have 3-7 items (actual: {plan.Agenda.Count}).");

            var totalMinutes = plan.Agenda.Sum(a => a.DurationMinutes);
            if (totalMinutes != 35)
                violations.Add($"Agenda duration must total 35 minutes (actual: {totalMinutes}).");

            var shortItems = plan.Agenda.Where(a => a.DurationMinutes < 3).ToList();
            if (shortItems.Count > 0)
                violations.Add($"Agenda items must be at least 3 minutes ({shortItems.Count} item(s) too short).");

            if (plan.PreworkTasks.Count < 2 || plan.PreworkTasks.Count > 5)
                violations.Add($"PreworkTasks must have 2-5 items (actual: {plan.PreworkTasks.Count}).");

            if (plan.FollowUpTasks.Count < 2 || plan.FollowUpTasks.Count > 5)
                violations.Add($"FollowUpTasks must have 2-5 items (actual: {plan.FollowUpTasks.Count}).");

            var longPrework = plan.PreworkTasks.Where(t => t.Length > 200).ToList();
            if (longPrework.Count > 0)
                violations.Add($"PreworkTasks items must be at most 200 characters ({longPrework.Count} item(s) too long).");

            var longFollowup = plan.FollowUpTasks.Where(t => t.Length > 200).ToList();
            if (longFollowup.Count > 0)
                violations.Add($"FollowUpTasks items must be at most 200 characters ({longFollowup.Count} item(s) too long).");
        }
    }

    /// <summary>
    /// Checks all text fields in the session plan for PII patterns.
    /// Detects: email addresses, phone numbers, physical addresses,
    /// Social Security Numbers, and credit card numbers.
    /// </summary>
    private void ValidateNoPiiPresent(SessionPlan plan, List<string> violations)
    {
        var allTextFields = GetAllTextFields(plan);

        foreach (var (fieldName, text) in allTextFields)
        {
            if (string.IsNullOrEmpty(text))
                continue;

            if (EmailRegex().IsMatch(text))
                violations.Add($"PII detected in {fieldName}: email address found.");

            if (PhoneRegex().IsMatch(text))
                violations.Add($"PII detected in {fieldName}: phone number found.");

            if (SsnRegex().IsMatch(text))
                violations.Add($"PII detected in {fieldName}: SSN pattern found.");

            if (CreditCardRegex().IsMatch(text))
                violations.Add($"PII detected in {fieldName}: credit card number found.");

            if (AddressRegex().IsMatch(text))
                violations.Add($"PII detected in {fieldName}: physical address pattern found.");
        }
    }

    /// <summary>
    /// Checks all text fields for harmful content patterns that Bedrock Guardrails
    /// might have missed (defense-in-depth). Looks for slurs, threats, explicit content markers.
    /// </summary>
    private static void ValidateNoHarmfulContent(SessionPlan plan, List<string> violations)
    {
        var allTextFields = GetAllTextFields(plan);

        foreach (var (fieldName, text) in allTextFields)
        {
            if (string.IsNullOrEmpty(text))
                continue;

            if (ContainsHarmfulContent(text))
                violations.Add($"Harmful content detected in {fieldName}.");
        }
    }

    /// <summary>
    /// Extracts all text fields from a session plan for inspection.
    /// Returns tuples of (fieldName, textValue) for reporting.
    /// </summary>
    private static IReadOnlyList<(string FieldName, string Text)> GetAllTextFields(SessionPlan plan)
    {
        var fields = new List<(string, string)>
        {
            ("SessionTitle", plan.SessionTitle)
        };

        for (var i = 0; i < plan.Agenda.Count; i++)
        {
            fields.Add(($"Agenda[{i}].Title", plan.Agenda[i].Title));
            fields.Add(($"Agenda[{i}].Description", plan.Agenda[i].Description));
        }

        for (var i = 0; i < plan.PreworkTasks.Count; i++)
        {
            fields.Add(($"PreworkTasks[{i}]", plan.PreworkTasks[i]));
        }

        for (var i = 0; i < plan.FollowUpTasks.Count; i++)
        {
            fields.Add(($"FollowUpTasks[{i}]", plan.FollowUpTasks[i]));
        }

        return fields;
    }

    /// <summary>
    /// Checks if text contains harmful content indicators.
    /// This is a defense-in-depth layer — Bedrock Guardrails handle the primary filtering,
    /// but we check for patterns that may have been obfuscated or missed.
    /// </summary>
    private static bool ContainsHarmfulContent(string text)
    {
        // Check for common harmful content indicators (case-insensitive)
        foreach (var pattern in HarmfulContentPatterns)
        {
            if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Patterns indicating potentially harmful content that should not appear
    /// in a professional mentorship session plan.
    /// </summary>
    private static readonly string[] HarmfulContentPatterns =
    [
        "kill yourself",
        "commit suicide",
        "self-harm",
        "hate speech",
        "racial slur",
        "bomb threat",
        "terrorist",
        "explicit sexual",
        "child abuse",
        "drug trafficking"
    ];

    // ── PII Detection Regex Patterns ──────────────────────────────────────────

    /// <summary>
    /// Matches email addresses (RFC 5322 simplified).
    /// Example: user@example.com, name.surname@domain.co.au
    /// </summary>
    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    /// <summary>
    /// Matches phone numbers in various formats:
    /// - Australian: 04xx xxx xxx, +61 4xx xxx xxx, (02) xxxx xxxx
    /// - International: +1-xxx-xxx-xxxx, xxx.xxx.xxxx
    /// </summary>
    [GeneratedRegex(@"(?:\+?\d{1,3}[-.\s]?)?\(?\d{2,4}\)?[-.\s]?\d{3,4}[-.\s]?\d{3,4}", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    /// <summary>
    /// Matches US Social Security Numbers: xxx-xx-xxxx or xxxxxxxxx.
    /// </summary>
    [GeneratedRegex(@"\b\d{3}[-\s]?\d{2}[-\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnRegex();

    /// <summary>
    /// Matches credit card numbers (13-19 digits with optional separators).
    /// Covers Visa, MasterCard, Amex, Discover patterns.
    /// </summary>
    [GeneratedRegex(@"\b(?:\d{4}[-\s]?){3,4}\d{0,4}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardRegex();

    /// <summary>
    /// Matches common physical address patterns:
    /// - Street number + street name + type (St, Rd, Ave, Dr, etc.)
    /// - PO Box patterns
    /// </summary>
    [GeneratedRegex(@"\b\d{1,5}\s+[A-Za-z]+\s+(?:Street|St|Road|Rd|Avenue|Ave|Drive|Dr|Boulevard|Blvd|Lane|Ln|Court|Ct|Place|Pl|Crescent|Cres|Way|Terrace|Tce)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AddressRegex();
}
