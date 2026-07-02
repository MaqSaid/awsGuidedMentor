using GuidedMentor.Identity.Infrastructure.Lambdas.Models;

namespace GuidedMentor.Identity.Infrastructure.Lambdas;

/// <summary>
/// Cognito "Define Auth Challenge" trigger. Determines what challenge to present.
/// Always returns MAGIC_LINK as the challenge type.
/// </summary>
public sealed class DefineAuthChallengeLambda
{
    public DefineAuthChallengeResponse Handler(DefineAuthChallengeRequest request)
    {
        // If user has already answered successfully, issue tokens
        if (request.Session.Any(s => s.ChallengeResult))
        {
            return new DefineAuthChallengeResponse
            {
                IssueTokens = true,
                FailAuthentication = false
            };
        }

        // Otherwise, present the CUSTOM_CHALLENGE
        return new DefineAuthChallengeResponse
        {
            IssueTokens = false,
            FailAuthentication = false,
            ChallengeName = "CUSTOM_CHALLENGE"
        };
    }
}
