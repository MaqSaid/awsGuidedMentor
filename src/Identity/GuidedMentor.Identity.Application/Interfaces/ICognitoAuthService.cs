using GuidedMentor.Identity.Application.DTOs;

namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Abstraction over Amazon Cognito authentication operations.
/// Implementation lives in the Infrastructure layer.
/// </summary>
public interface ICognitoAuthService
{
    /// <summary>
    /// Creates a new user with email/password in Cognito and sends verification email.
    /// </summary>
    Task<AuthResult> SignUpWithEmailAsync(string email, string password, CancellationToken ct);

    /// <summary>
    /// Verifies an email address using the 6-digit code sent during signup.
    /// </summary>
    Task<AuthResult> VerifyEmailAsync(string email, string code, CancellationToken ct);

    /// <summary>
    /// Authenticates a user with email and password, returning JWT tokens on success.
    /// </summary>
    Task<AuthResult> SignInAsync(string email, string password, CancellationToken ct);

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
