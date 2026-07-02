using System.Text.Json;

namespace GuidedMentor.Engagement.Application.Services;

/// <summary>
/// Provides FAQ lookup functionality by matching user messages against
/// pre-defined FAQ entries using keyword-based scoring. When a match is found
/// above the confidence threshold, the response is served directly without
/// invoking Amazon Bedrock, saving tokens for common questions.
/// </summary>
public sealed class FaqLookupService
{
    private readonly IReadOnlyList<FaqEntry> _entries;

    public FaqLookupService()
    {
        var json = LoadEmbeddedResource();
        _entries = JsonSerializer.Deserialize<List<FaqEntry>>(json, FaqJsonOptions.Default) ?? [];
    }

    /// <summary>
    /// Attempts to find a FAQ entry matching the user's message.
    /// Returns null if no match exceeds the confidence threshold.
    /// </summary>
    /// <param name="userMessage">The user's input message.</param>
    /// <param name="threshold">Minimum confidence (0.0–1.0) required for a match. Default is 0.5.</param>
    public FaqMatch? FindMatch(string userMessage, double threshold = 0.5)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return null;

        var normalized = NormalizeText(userMessage);

        FaqMatch? bestMatch = null;

        foreach (var entry in _entries)
        {
            var matchedKeywords = entry.Keywords.Count(kw => normalized.Contains(kw.ToLowerInvariant()));
            if (matchedKeywords == 0) continue;

            var confidence = (double)matchedKeywords / entry.Keywords.Length;

            if (confidence >= threshold && (bestMatch is null || confidence > bestMatch.Confidence))
            {
                bestMatch = new FaqMatch(entry.Id, entry.Question, entry.Answer, confidence);
            }
        }

        return bestMatch;
    }

    internal static string NormalizeText(string text)
    {
        return text.ToLowerInvariant()
            .Replace("?", "")
            .Replace("!", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Trim();
    }

    private static string LoadEmbeddedResource()
    {
        var assembly = typeof(FaqLookupService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("faq-entries.json"))
            ?? throw new InvalidOperationException("faq-entries.json embedded resource not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static class FaqJsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
