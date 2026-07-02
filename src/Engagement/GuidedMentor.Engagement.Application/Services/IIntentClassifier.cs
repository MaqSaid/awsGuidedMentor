namespace GuidedMentor.Engagement.Application.Services;

/// <summary>
/// Classifies user messages into intent categories to optimize routing.
/// Off-topic messages are rejected without calling Bedrock.
/// Navigation questions use a minimal system prompt.
/// </summary>
public interface IIntentClassifier
{
    Task<ChatIntent> ClassifyAsync(string message, CancellationToken ct = default);
}

/// <summary>
/// Intent categories used to route messages through the AI Help Assistant pipeline.
/// </summary>
public enum ChatIntent
{
    /// <summary>Questions about platform features, how things work, troubleshooting.</summary>
    PlatformHelp,

    /// <summary>Simple navigation questions (where is X, how to get to Y).</summary>
    Navigation,

    /// <summary>Questions unrelated to the platform (coding help, weather, news, personal).</summary>
    OffTopic
}
