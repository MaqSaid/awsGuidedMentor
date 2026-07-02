namespace GuidedMentor.Identity.Infrastructure.Lambdas.Models;

/// <summary>
/// Request model for Cognito Create Auth Challenge trigger.
/// </summary>
public sealed class CreateAuthChallengeRequest
{
    public IDictionary<string, string> UserAttributes { get; set; } = new Dictionary<string, string>();
    public string ChallengeName { get; set; } = string.Empty;
    public string Session { get; set; } = string.Empty;
}

/// <summary>
/// Response model for Cognito Create Auth Challenge trigger.
/// </summary>
public sealed class CreateAuthChallengeResponse
{
    public IDictionary<string, string> PublicChallengeParameters { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> PrivateChallengeParameters { get; set; } = new Dictionary<string, string>();
}
