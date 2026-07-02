using GuidedMentor.Engagement.Application.Plugins;
using GuidedMentor.Engagement.Application.Services;
using MediatR;
using Microsoft.Extensions.AI;
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
    private readonly FaqLookupService _faqLookupService;
    private readonly IIntentClassifier _intentClassifier;
    private readonly IChatRateLimiter _rateLimiter;
    private readonly ILogger<ChatWithAssistantHandler> _logger;

    public ChatWithAssistantHandler(
        HelpAssistantPlugin helpAssistantPlugin,
        FaqLookupService faqLookupService,
        IIntentClassifier intentClassifier,
        IChatRateLimiter rateLimiter,
        ILogger<ChatWithAssistantHandler> logger)
    {
        _helpAssistantPlugin = helpAssistantPlugin ?? throw new ArgumentNullException(nameof(helpAssistantPlugin));
        _faqLookupService = faqLookupService ?? throw new ArgumentNullException(nameof(faqLookupService));
        _intentClassifier = intentClassifier ?? throw new ArgumentNullException(nameof(intentClassifier));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatWithAssistantResult> Handle(
        ChatWithAssistantCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate non-empty message
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            _logger.LogWarning("Empty message received from UserId={UserId}", request.UserId);
            return ChatWithAssistantResult.InvalidInput("Message cannot be empty.");
        }

        // 2. Check rate limit (20 messages/minute/user)
        if (!_rateLimiter.IsAllowed(request.UserId))
        {
            var remaining = _rateLimiter.GetRemainingMessages(request.UserId);

            _logger.LogWarning(
                "Rate limit exceeded for UserId={UserId}. Remaining={Remaining}",
                request.UserId, remaining);

            return ChatWithAssistantResult.RateLimited(remaining);
        }

        // 3. Sanitize input: max 1000 chars, strip injection patterns, control chars
        var sanitizedMessage = InputSanitizer.Sanitize(request.Message);

        if (string.IsNullOrWhiteSpace(sanitizedMessage))
        {
            _logger.LogWarning(
                "Message became empty after sanitization for UserId={UserId}",
                request.UserId);
            return ChatWithAssistantResult.InvalidInput("Message contains no valid content after processing.");
        }

        // Log if injection was attempted (for security monitoring)
        if (InputSanitizer.ContainsInjectionPattern(request.Message))
        {
            _logger.LogWarning(
                "Prompt injection pattern detected from UserId={UserId}. Original length={Length}",
                request.UserId, request.Message.Length);
        }

        // 4. Check FAQ first (zero Bedrock tokens if matched)
        var faqMatch = _faqLookupService.FindMatch(sanitizedMessage);
        if (faqMatch is not null)
        {
            _logger.LogInformation(
                "FAQ match found. UserId={UserId}, FaqId={FaqId}, Confidence={Confidence:F2}",
                request.UserId, faqMatch.FaqId, faqMatch.Confidence);

            var faqStream = StreamFaqAnswer(faqMatch.Answer);
            var remainingAfterFaq = _rateLimiter.GetRemainingMessages(request.UserId);
            return ChatWithAssistantResult.Success(faqStream, remainingAfterFaq);
        }

        // 5. Classify intent via Hugging Face (off-topic rejection + prompt routing)
        var intent = await _intentClassifier.ClassifyAsync(sanitizedMessage, cancellationToken);

        switch (intent)
        {
            case ChatIntent.OffTopic:
                _logger.LogInformation(
                    "Off-topic message rejected. UserId={UserId}",
                    request.UserId);
                var rejectStream = StreamFaqAnswer(SystemPromptSubsets.OffTopicRejection);
                var remainingAfterReject = _rateLimiter.GetRemainingMessages(request.UserId);
                return ChatWithAssistantResult.Success(rejectStream, remainingAfterReject);

            case ChatIntent.Navigation:
                _logger.LogInformation(
                    "Navigation intent detected. Using minimal prompt. UserId={UserId}",
                    request.UserId);
                var navStream = _helpAssistantPlugin.StreamResponseAsync(
                    sanitizedMessage, request.History, SystemPromptSubsets.NavigationOnly, cancellationToken);
                var remainingAfterNav = _rateLimiter.GetRemainingMessages(request.UserId);
                return ChatWithAssistantResult.Success(navStream, remainingAfterNav);

            case ChatIntent.PlatformHelp:
            default:
                // Fall through to existing full Bedrock path (step 6)
                break;
        }

        // 6. Stream response from the HelpAssistantPlugin (full system prompt)
        _logger.LogInformation(
            "Processing AI Help Assistant request. UserId={UserId}, MessageLength={Length}, HistoryCount={HistoryCount}",
            request.UserId, sanitizedMessage.Length, request.History.Count);

        var responseStream = _helpAssistantPlugin.StreamResponseAsync(
            sanitizedMessage,
            request.History,
            cancellationToken);

        var remainingMessages = _rateLimiter.GetRemainingMessages(request.UserId);

        return ChatWithAssistantResult.Success(responseStream, remainingMessages);
    }

    /// <summary>
    /// Wraps a FAQ answer as an IAsyncEnumerable for compatibility with the existing
    /// SSE streaming response infrastructure. Yields the complete answer in a single chunk.
    /// </summary>
    private static async IAsyncEnumerable<string> StreamFaqAnswer(string answer)
    {
        yield return answer;
        await Task.CompletedTask; // Maintain async signature
    }
}
