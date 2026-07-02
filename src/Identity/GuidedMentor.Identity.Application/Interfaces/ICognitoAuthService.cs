using GuidedMentor.Identity.Application.DTOs;

namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Abstraction over Amazon Cognito authentication operations.
/// Implementation lives in the Infrastructure layer.
/// </summary>
public interface ICognitoAuthService
{
    /// <summary>
    /// Initiates the CUSTOM_AUTH flow for magic link authentication.
    /// Triggers the DefineAuthChallenge and CreateAuthChallenge Lambda triggers.
    /// </summary>
    Task<AuthResult> InitiateCustomAuthAsync(string email, CancellationToken ct);

    /// <summary>
    /// Responds to the custom auth challenge with the magic link token.
    /// On success, Cognito issues JWT tokens.
    /// </summary>
    Task<AuthResult> RespondToCustomChallengeAsync(string email, string token, string session, CancellationToken ct);

    /// <summary>
    /// Invalidates all tokens for the user (global sign-out).
    /// </summary>
    Task SignOutAsync(string accessToken, CancellationToken ct);

    /// <summary>
    /// Exchanges a refresh token for a new access token.
    /// </summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct);

    /// <summary>
    /// Exchanges a Google OAuth authorization code for Cognito tokens.
    /// </summary>
    Task<AuthResult> ExchangeGoogleCodeAsync(string code, CancellationToken ct);
}
