using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.Interfaces;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Handles Google OAuth signup by exchanging the authorization code for tokens via Cognito.
/// On success, creates/retrieves the user record, issues JWT, and redirects to role selection.
/// </summary>
public sealed class GoogleOAuthSignupHandler : IRequestHandler<GoogleOAuthSignupCommand, AuthResponse>
{
    private readonly ICognitoAuthService _cognitoAuthService;

    public GoogleOAuthSignupHandler(ICognitoAuthService cognitoAuthService)
    {
        _cognitoAuthService = cognitoAuthService;
    }

    public async Task<AuthResponse> Handle(GoogleOAuthSignupCommand request, CancellationToken cancellationToken)
    {
        var result = await _cognitoAuthService.ExchangeGoogleCodeAsync(request.AuthorizationCode, cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                result.ErrorMessage ?? "Authentication with Google was not completed.");
        }

        return new AuthResponse(
            AccessToken: result.AccessToken!,
            RefreshToken: result.RefreshToken!,
            IdToken: result.IdToken!,
            ActiveRole: null, // New user → redirect to role selection
            ExpiresIn: result.ExpiresIn);
    }
}
