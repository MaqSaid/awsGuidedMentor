namespace GuidedMentor.Identity.Infrastructure.Lambdas.Models;

/// <summary>
/// Request model for Cognito Verify Auth Challenge trigger.
/// </summary>
public sealed class VerifyAuthChallengeRequest
{
    public string ChallengeAnswer { get; set; } = string.Empty;
    public IDictionary<string, string> UserAttributes { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> PrivateChallengeParameters { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Response model for Cognito Verify Auth Challenge trigger.
/// </summary>
public sealed class VerifyAuthChallengeResponse
{
    public bool AnswerCorrect { get; set; }
}
