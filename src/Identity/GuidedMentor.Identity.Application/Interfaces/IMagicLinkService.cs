using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Service interface for magic link operations.
/// Implementation uses Cognito Custom Auth + DynamoDB + SES.
/// </summary>
public interface IMagicLinkService
{
    /// <summary>
    /// Checks rate limit: max 3 requests per email per 15 minutes.
    /// </summary>
    Task<bool> CanSendAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Initiates Cognito CUSTOM_AUTH flow which triggers the Create Auth Challenge Lambda.
    /// </summary>
    Task SendMagicLinkAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Responds to the Cognito auth challenge with the token, returning JWT tokens on success.
    /// </summary>
    Task<Result<AuthResponse>> VerifyAndAuthenticateAsync(string email, string token, CancellationToken ct = default);
}
