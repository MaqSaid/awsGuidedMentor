namespace GuidedMentor.Engagement.Application.Services;

/// <summary>
/// Represents a single FAQ entry loaded from the embedded JSON resource.
/// </summary>
public sealed record FaqEntry(string Id, string[] Keywords, string Question, string Answer);

/// <summary>
/// Represents a matched FAQ result with confidence score.
/// Returned when the user's message matches FAQ keywords above the threshold.
/// </summary>
public sealed record FaqMatch(string FaqId, string Question, string Answer, double Confidence);
