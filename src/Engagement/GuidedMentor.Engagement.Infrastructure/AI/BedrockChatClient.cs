using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Engagement.Infrastructure.AI;

/// <summary>
/// Placeholder IChatClient implementation. In production this will be replaced by
/// an actual LLM provider. In local dev, MockChatClient overrides this registration.
/// </summary>
public sealed class PlaceholderChatClient : IChatClient
{
    private readonly ILogger<PlaceholderChatClient> _logger;

    public PlaceholderChatClient(ILogger<PlaceholderChatClient> logger)
    {
        _logger = logger;
    }

    public ChatClientMetadata Metadata => new("PlaceholderChatClient", null, "placeholder");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("PlaceholderChatClient invoked — no AI provider configured");

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant,
            "I'm currently unavailable. Please try again later."));
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("PlaceholderChatClient streaming invoked — no AI provider configured");
        yield return new ChatResponseUpdate(ChatRole.Assistant, "I'm currently unavailable.");
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        // No resources to dispose
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(IChatClient))
            return this;
        return null;
    }
}
