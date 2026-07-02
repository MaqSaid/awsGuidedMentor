using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace GuidedMentor.LocalDev.Mocks;

/// <summary>
/// Mock IChatClient that returns canned responses without calling Bedrock.
/// Zero tokens consumed during local development.
/// </summary>
public sealed class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("mock-local-dev");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant,
            "This is a mock response from the local development AI. " +
            "In production, this would be powered by Claude Sonnet 4 via Amazon Bedrock. " +
            "Your question was received and processed successfully."));
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var words = "This is a mock streaming response from the local AI. In production, this would be Claude Sonnet 4.".Split(' ');
        foreach (var word in words)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ");
            await Task.Delay(50, cancellationToken); // Simulate streaming delay
        }
    }

    public void Dispose()
    {
        // No resources to dispose in mock
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(IChatClient))
            return this;
        return null;
    }
}
