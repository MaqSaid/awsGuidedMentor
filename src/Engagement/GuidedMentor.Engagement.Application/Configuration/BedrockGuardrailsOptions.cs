namespace GuidedMentor.Engagement.Application.Configuration;

/// <summary>
/// Configuration options for Amazon Bedrock Guardrails applied to the AI Help Assistant.
/// These guardrails prevent: system prompt disclosure, harmful content generation,
/// responses about unrelated topics, and leakage of other users' data.
/// 
/// Validates: Requirements 14.10
/// </summary>
public sealed class BedrockGuardrailsOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "BedrockGuardrails";

    /// <summary>
    /// The ARN of the Bedrock Guardrail to apply to AI Help Assistant requests.
    /// Example: arn:aws:bedrock:ap-southeast-2:123456789012:guardrail/abc123
    /// </summary>
    public string GuardrailIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The version of the guardrail to use.
    /// Use "DRAFT" for development, specific version numbers for production.
    /// </summary>
    public string GuardrailVersion { get; set; } = "DRAFT";

    /// <summary>
    /// Whether guardrails are enabled. Set to false in development/testing if guardrail
    /// is not provisioned yet.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Content filter strength for hate/insults/sexual/violence categories.
    /// Valid values: NONE, LOW, MEDIUM, HIGH.
    /// </summary>
    public string ContentFilterStrength { get; set; } = "HIGH";

    /// <summary>
    /// Denied topics that the guardrail blocks. These are configured in the Bedrock console
    /// and referenced here for documentation purposes.
    /// </summary>
    /// <remarks>
    /// Configured denied topics:
    /// - "system_prompt_disclosure": Blocks attempts to extract or reveal system instructions
    /// - "off_platform_topics": Blocks responses about topics unrelated to GuidedMentor
    /// - "other_user_data": Blocks attempts to extract information about other users
    /// - "harmful_content": Blocks generation of harmful, toxic, or dangerous content
    /// </remarks>
    public IReadOnlyList<string> DeniedTopics { get; set; } =
    [
        "system_prompt_disclosure",
        "off_platform_topics",
        "other_user_data",
        "harmful_content"
    ];

    /// <summary>
    /// Word filters to block profanity and specific terms.
    /// </summary>
    public IReadOnlyList<string> BlockedWords { get; set; } = [];

    /// <summary>
    /// Whether PII redaction is enabled (masks email, phone, addresses from AI outputs).
    /// </summary>
    public bool PiiRedactionEnabled { get; set; } = true;
}
