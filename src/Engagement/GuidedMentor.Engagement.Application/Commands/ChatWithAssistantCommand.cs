using MediatR;
using Microsoft.Extensions.AI;

namespace GuidedMentor.Engagement.Application.Commands;

/// <summary>
/// Command to send a message to the AI Help Assistant and receive a streaming response.
/// The handler sanitizes the input, checks rate limits, invokes the HelpAssistantPlugin,
/// and returns a streaming result.
/// 
/// Validates: Requirements 14.2, 14.3, 14.4, 14.9, 14.11
/// </summary>
public sealed record ChatWithAssistantCommand(
    Guid UserId,
    string Message,
    IReadOnlyList<ChatMessage> History
) : IRequest<ChatWithAssistantResult>;

/// <summary>
/// Result of the ChatWithAssistantCommand. Contains either a streaming response
/// or an error indicating why the request was rejected.
/// </summary>
public sealed class ChatWithAssistantResult
{
    /// <summary>
    /// Whether the request was accepted and a response stream is available.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Error message when the request is rejected (rate limited, invalid input, etc.).
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// The async enumerable of response text chunks for streaming to the client.
    /// Only populated when <see cref="IsSuccess"/> is true.
    /// </summary>
    public IAsyncEnumerable<string>? ResponseStream { get; }

    /// <summary>
    /// Number of remaining messages the user can send in the current rate limit window.
    /// </summary>
    public int RemainingMessages { get; }

    private ChatWithAssistantResult(bool isSuccess, string error, IAsyncEnumerable<string>? responseStream, int remainingMessages)
    {
        IsSuccess = isSuccess;
        Error = error;
        ResponseStream = responseStream;
        RemainingMessages = remainingMessages;
    }

    public static ChatWithAssistantResult Success(IAsyncEnumerable<string> stream, int remainingMessages) =>
        new(true, string.Empty, stream, remainingMessages);

    public static ChatWithAssistantResult RateLimited(int remainingMessages) =>
        new(false, "Rate limit exceeded. You can send up to 20 messages per minute. Please wait a moment before trying again.", null, remainingMessages);

    public static ChatWithAssistantResult InvalidInput(string reason) =>
        new(false, reason, null, 0);

    public static ChatWithAssistantResult Failure(string error) =>
        new(false, error, null, 0);
}
