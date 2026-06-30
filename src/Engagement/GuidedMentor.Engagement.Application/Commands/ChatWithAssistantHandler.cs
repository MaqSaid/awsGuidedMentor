using GuidedMentor.Engagement.Application.Plugins;
using GuidedMentor.Engagement.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Engagement.Application.Commands;

/// <summary>
/// Handles the ChatWithAssistantCommand by:
/// 1. Validating that the user message is non-empty
/// 2. Checking per-user rate limits (20 messages/minute)
/// 3. Sanitizing input (max 1000 chars, strip injection patterns, control chars)
/// 4. Invoking the HelpAssistantPlugin to stream the response via IChatClient
/// 
/// The streaming response is returned as an IAsyncEnumerable&lt;string&gt; which
/// the API layer can forward as Server-Sent Events for the frontend's useChat() hook.
/// 
/// Validates: Requirements 14.2, 14.3, 14.4, 14.5, 14.6, 14.9, 14.11
/// </summary>
public sealed class ChatWithAssistantHandler : IRequestHandler<ChatWithAssistantCommand, ChatWithAssistantResult>
{
    private readonly HelpAssistantPlugin _helpAssistantPlugin;
    private readonly IChatRateLimiter _rateLimiter;
    private readonly ILogger<ChatWithAssistantHandler> _logger;

    public ChatWithAssistantHandler(
        HelpAssistantPlugin helpAssistantPlugin,
        IChatRateLimiter rateLimiter,
        ILogger<ChatWithAssistantHandler> logger)
    {
        _helpAssistantPlugin = helpAssistantPlugin ?? throw new ArgumentNullException(nameof(helpAssistantPlugin));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<ChatWithAssistantResult> Handle(
        ChatWithAssistantCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate non-empty message
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            _logger.LogWarning("Empty message received from UserId={UserId}", request.UserId);
            return Task.FromResult(
                ChatWithAssistantResult.InvalidInput("Message cannot be empty."));
        }

        // 2. Check rate limit (20 messages/minute/user)
        if (!_rateLimiter.IsAllowed(request.UserId))
        {
            var remaining = _rateLimiter.GetRemainingMessages(request.UserId);

            _logger.LogWarning(
                "Rate limit exceeded for UserId={UserId}. Remaining={Remaining}",
                request.UserId, remaining);

            return Task.FromResult(ChatWithAssistantResult.RateLimited(remaining));
        }

        // 3. Sanitize input: max 1000 chars, strip injection patterns, control chars
        var sanitizedMessage = InputSanitizer.Sanitize(request.Message);

        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _logger.LogWarning(
                "Message became empty after sanitization for UserId={UserId}",
                request.UserId);
            return Task.FromResult(
                ChatWithAssistantResult.InvalidInput("Message contains no valid content after processing."));
        }

        // Log if injection was attempted (for security monitoring)
        if (InputSanitizer.ContainsInjectionPattern(request.Message))
        {
            _logger.LogWarning(
                "Prompt injection pattern detected from UserId={UserId}. Original length={Length}",
                request.UserId, request.Message.Length);
        }

        // 4. Stream response from the HelpAssistantPlugin
        _logger.LogInformation(
            "Processing AI Help Assistant request. UserId={UserId}, MessageLength={Length}, HistoryCount={HistoryCount}",
            request.UserId, sanitizedMessage.Length, request.History.Count);

        var responseStream = _helpAssistantPlugin.StreamResponseAsync(
            sanitizedMessage,
            request.History,
            cancellationToken);

        var remainingMessages = _rateLimiter.GetRemainingMessages(request.UserId);

        return Task.FromResult(ChatWithAssistantResult.Success(responseStream, remainingMessages));
    }
}
