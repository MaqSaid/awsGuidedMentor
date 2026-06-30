using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Handles silent token refresh by exchanging the refresh token for a new access token via Cognito.
/// </summary>
public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly ICognitoAuthService _cognitoAuthService;

    public RefreshTokenHandler(ICognitoAuthService cognitoAuthService)
    {
        _cognitoAuthService = cognitoAuthService;
    }

    public async Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _cognitoAuthService.RefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Result<AuthResponse>.Failure(
                result.ErrorMessage ?? "Token refresh failed.");
        }

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: result.AccessToken!,
            RefreshToken: result.RefreshToken ?? request.RefreshToken, // Cognito may not return a new refresh token
            IdToken: result.IdToken ?? string.Empty,
            ActiveRole: null,
            ExpiresIn: result.ExpiresIn));
    }
}
