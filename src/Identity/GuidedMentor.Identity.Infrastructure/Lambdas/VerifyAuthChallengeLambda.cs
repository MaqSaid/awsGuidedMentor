namespace GuidedMentor.Identity.Infrastructure.Lambdas;

/// <summary>
/// Placeholder — Cognito Verify Auth Challenge Lambda is no longer used.
/// Token verification is now handled directly by the API against the PostgreSQL auth_tokens table.
/// </summary>
public sealed class VerifyAuthChallengePlaceholder
{
    // No-op: Token verification logic moved to API layer.
    // The API queries: SELECT * FROM auth_tokens WHERE token = @token AND used = false AND expires_at > NOW()
    // Then marks the token as used: UPDATE auth_tokens SET used = true WHERE token = @token
}
