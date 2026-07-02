using GuidedMentor.Engagement.Application.Services;

namespace GuidedMentor.LocalDev.Mocks;

/// <summary>
/// Mock intent classifier for local dev. Always returns PlatformHelp
/// so all messages go through the full pipeline (FAQ → Bedrock mock).
/// Skips the HuggingFace API call entirely.
/// </summary>
public sealed class MockIntentClassifier : IIntentClassifier
{
    public Task<ChatIntent> ClassifyAsync(string message, CancellationToken ct = default)
    {
        // In local dev, let everything through to test the full flow
        return Task.FromResult(ChatIntent.PlatformHelp);
    }
}
