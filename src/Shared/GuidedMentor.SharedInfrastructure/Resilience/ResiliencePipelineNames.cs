namespace GuidedMentor.SharedInfrastructure.Resilience;

/// <summary>
/// Named resilience pipeline identifiers for Polly v8 dependency-specific configurations.
/// </summary>
public static class ResiliencePipelineNames
{
    public const string Bedrock = "bedrock";
    public const string DynamoDb = "dynamodb";
    public const string Aurora = "aurora";
}
