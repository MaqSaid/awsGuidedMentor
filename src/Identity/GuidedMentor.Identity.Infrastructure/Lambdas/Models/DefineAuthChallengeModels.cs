namespace GuidedMentor.Identity.Infrastructure.Lambdas.Models;

/// <summary>
/// Request model for Cognito Define Auth Challenge trigger.
/// </summary>
public sealed class DefineAuthChallengeRequest
{
    public IList<ChallengeSession> Session { get; set; } = [];
    public IDictionary<string, string> UserAttributes { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents a single challenge session entry.
/// </summary>
public sealed class ChallengeSession
{
    public string ChallengeName { get; set; } = string.Empty;
    public bool ChallengeResult { get; set; }
}

/// <summary>
/// Response model for Cognito Define Auth Challenge trigger.
/// </summary>
public sealed class DefineAuthChallengeResponse
{
    public bool IssueTokens { get; set; }
    public bool FailAuthentication { get; set; }
    public string? ChallengeName { get; set; }
}
