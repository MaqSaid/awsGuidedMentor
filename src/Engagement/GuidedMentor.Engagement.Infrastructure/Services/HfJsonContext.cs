using System.Text.Json.Serialization;

namespace GuidedMentor.Engagement.Infrastructure.Services;

/// <summary>
/// Request DTO for Hugging Face zero-shot classification API.
/// </summary>
public sealed record HfClassificationRequest
{
    [JsonPropertyName("inputs")]
    public string Inputs { get; init; } = string.Empty;

    [JsonPropertyName("parameters")]
    public HfParameters Parameters { get; init; } = new();
}

/// <summary>
/// Parameters for the Hugging Face classification request.
/// </summary>
public sealed record HfParameters
{
    [JsonPropertyName("candidate_labels")]
    public string[] CandidateLabels { get; init; } = [];
}

/// <summary>
/// Response DTO from Hugging Face zero-shot classification API.
/// </summary>
public sealed record HfClassificationResponse
{
    [JsonPropertyName("labels")]
    public string[] Labels { get; init; } = [];

    [JsonPropertyName("scores")]
    public double[] Scores { get; init; } = [];
}

/// <summary>
/// AOT-compatible JSON serialization context for Hugging Face API DTOs.
/// </summary>
[JsonSerializable(typeof(HfClassificationRequest))]
[JsonSerializable(typeof(HfParameters))]
[JsonSerializable(typeof(HfClassificationResponse))]
internal sealed partial class HfJsonContext : JsonSerializerContext
{
}
