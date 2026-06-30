using System.Runtime.CompilerServices;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using GuidedMentor.Engagement.Application.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuidedMentor.Engagement.Infrastructure.AI;

/// <summary>
/// IChatClient implementation backed by Amazon Bedrock Converse API (Claude Sonnet 4).
/// Supports both standard and streaming responses. Applies Bedrock Guardrails when configured.
/// 
/// Validates: Requirements 14.2, 14.10, 17.1, 17.5
/// </summary>
public sealed class BedrockChatClient : IChatClient
{
    private readonly IAmazonBedrockRuntime _bedrockRuntime;
    private readonly BedrockGuardrailsOptions _guardrailOptions;
    private readonly ILogger<BedrockChatClient> _logger;

    /// <summary>
    /// Model ID for Claude Sonnet 4 in ap-southeast-2.
    /// </summary>
    internal const string ModelId = "anthropic.claude-sonnet-4-20250514-v1:0";

    public BedrockChatClient(
        IAmazonBedrockRuntime bedrockRuntime,
        IOptions<BedrockGuardrailsOptions> guardrailOptions,
        ILogger<BedrockChatClient> logger)
    {
        _bedrockRuntime = bedrockRuntime ?? throw new ArgumentNullException(nameof(bedrockRuntime));
        _guardrailOptions = guardrailOptions?.Value ?? new BedrockGuardrailsOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ChatClientMetadata Metadata => new("BedrockChatClient", new Uri("https://bedrock.ap-southeast-2.amazonaws.com"), ModelId);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var request = BuildConverseRequest(messages);

        _logger.LogInformation("Invoking Bedrock Converse API. MessageCount={Count}", messages.Count);

        var response = await _bedrockRuntime.ConverseAsync(request, cancellationToken);

        var responseText = ExtractResponseText(response);

        _logger.LogInformation(
            "Bedrock Converse response received. InputTokens={Input}, OutputTokens={Output}",
            response.Usage?.InputTokens ?? 0,
            response.Usage?.OutputTokens ?? 0);

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText))
        {
            Usage = new UsageDetails
            {
                InputTokenCount = response.Usage?.InputTokens ?? 0,
                OutputTokenCount = response.Usage?.OutputTokens ?? 0
            }
        };

        return chatResponse;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = chatMessages.ToList();
        var request = BuildConverseStreamRequest(messages);

        _logger.LogInformation("Invoking Bedrock ConverseStream API. MessageCount={Count}", messages.Count);

        var response = await _bedrockRuntime.ConverseStreamAsync(request, cancellationToken);

        if (response.Stream is not null)
        {
            foreach (var eventItem in response.Stream.AsEnumerable())
            {
                if (eventItem is ContentBlockDeltaEvent deltaEvent && deltaEvent.Delta?.Text is not null)
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, deltaEvent.Delta.Text);
                }
            }
        }
    }

    public void Dispose()
    {
        // The IAmazonBedrockRuntime is managed by DI container
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(IChatClient))
            return this;
        return null;
    }

    private ConverseRequest BuildConverseRequest(List<ChatMessage> messages)
    {
        var (systemMessages, conversationMessages) = SplitMessages(messages);

        var request = new ConverseRequest
        {
            ModelId = ModelId,
            Messages = conversationMessages,
        };

        if (systemMessages.Count > 0)
        {
            request.System = systemMessages;
        }

        ApplyGuardrails(request);

        return request;
    }

    private ConverseStreamRequest BuildConverseStreamRequest(List<ChatMessage> messages)
    {
        var (systemMessages, conversationMessages) = SplitMessages(messages);

        var request = new ConverseStreamRequest
        {
            ModelId = ModelId,
            Messages = conversationMessages,
        };

        if (systemMessages.Count > 0)
        {
            request.System = systemMessages;
        }

        ApplyGuardrails(request);

        return request;
    }

    private (List<SystemContentBlock> system, List<Message> conversation) SplitMessages(List<ChatMessage> messages)
    {
        var systemBlocks = new List<SystemContentBlock>();
        var conversation = new List<Message>();

        foreach (var msg in messages)
        {
            if (msg.Role == ChatRole.System)
            {
                systemBlocks.Add(new SystemContentBlock { Text = msg.Text ?? string.Empty });
            }
            else
            {
                var role = msg.Role == ChatRole.Assistant
                    ? ConversationRole.Assistant
                    : ConversationRole.User;

                conversation.Add(new Message
                {
                    Role = role,
                    Content = [new ContentBlock { Text = msg.Text ?? string.Empty }]
                });
            }
        }

        return (systemBlocks, conversation);
    }

    private void ApplyGuardrails(ConverseRequest request)
    {
        if (!_guardrailOptions.Enabled || string.IsNullOrEmpty(_guardrailOptions.GuardrailIdentifier))
            return;

        request.GuardrailConfig = new GuardrailConfiguration
        {
            GuardrailIdentifier = _guardrailOptions.GuardrailIdentifier,
            GuardrailVersion = _guardrailOptions.GuardrailVersion
        };
    }

    private void ApplyGuardrails(ConverseStreamRequest request)
    {
        if (!_guardrailOptions.Enabled || string.IsNullOrEmpty(_guardrailOptions.GuardrailIdentifier))
            return;

        request.GuardrailConfig = new GuardrailStreamConfiguration
        {
            GuardrailIdentifier = _guardrailOptions.GuardrailIdentifier,
            GuardrailVersion = _guardrailOptions.GuardrailVersion
        };
    }

    private static string ExtractResponseText(ConverseResponse response)
    {
        if (response.Output?.Message?.Content is null)
            return string.Empty;

        return string.Join("", response.Output.Message.Content
            .Where(c => c.Text is not null)
            .Select(c => c.Text));
    }
}
