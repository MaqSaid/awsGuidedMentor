using System.Net.Http.Json;
using GuidedMentor.Engagement.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuidedMentor.Engagement.Infrastructure.Services;

/// <summary>
/// Uses Hugging Face Inference API (facebook/bart-large-mnli) for zero-shot
/// text classification to route user messages by intent.
/// Falls back to PlatformHelp if the API is unreachable (graceful degradation).
/// </summary>
public sealed class HuggingFaceIntentClassifier : IIntentClassifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceIntentClassifier> _logger;
    private readonly HuggingFaceOptions _options;

    internal static readonly string[] CandidateLabels =
    [
        "platform help and troubleshooting",
        "navigation and finding pages",
        "off-topic unrelated question"
    ];

    public HuggingFaceIntentClassifier(
        HttpClient httpClient,
        IOptions<HuggingFaceOptions> options,
        ILogger<HuggingFaceIntentClassifier> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatIntent> ClassifyAsync(string message, CancellationToken ct = default)
    {
        try
        {
            var requestBody = new HfClassificationRequest
            {
                Inputs = message,
                Parameters = new HfParameters { CandidateLabels = CandidateLabels }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"https://api-inference.huggingface.co/models/{_options.ClassificationModel}",
                requestBody,
                HfJsonContext.Default.HfClassificationRequest,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "HuggingFace API returned {StatusCode}. Falling back to PlatformHelp.",
                    response.StatusCode);
                return ChatIntent.PlatformHelp; // Graceful degradation
            }

            var result = await response.Content.ReadFromJsonAsync(
                HfJsonContext.Default.HfClassificationResponse, ct);

            if (result is null || result.Labels.Length == 0)
            {
                return ChatIntent.PlatformHelp;
            }

            // Map the top label to our enum
            var topLabel = result.Labels[0];
            var topScore = result.Scores[0];

            _logger.LogInformation(
                "HF Intent Classification: TopLabel={Label}, Score={Score:F3}, Message={Message}",
                topLabel, topScore, message[..Math.Min(50, message.Length)]);

            // Only classify as off-topic or navigation if confidence is high (>0.7)
            if (topScore < 0.7)
                return ChatIntent.PlatformHelp;

            return topLabel switch
            {
                "off-topic unrelated question" => ChatIntent.OffTopic,
                "navigation and finding pages" => ChatIntent.Navigation,
                _ => ChatIntent.PlatformHelp
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HuggingFace intent classification failed. Falling back to PlatformHelp.");
            return ChatIntent.PlatformHelp; // Graceful degradation — never block the user
        }
    }
}

/// <summary>
/// Configuration options for Hugging Face Inference API.
/// </summary>
public sealed class HuggingFaceOptions
{
    public const string SectionName = "HuggingFace";
    public string ApiKey { get; set; } = string.Empty;
    public string ClassificationModel { get; set; } = "facebook/bart-large-mnli";
}
