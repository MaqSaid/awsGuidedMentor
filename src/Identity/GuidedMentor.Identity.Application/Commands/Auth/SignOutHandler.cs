using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Handles sign-out by revoking all tokens via Cognito GlobalSignOut.
/// </summary>
public sealed class SignOutHandler : IRequestHandler<SignOutCommand, Result>
{
    private readonly ICognitoAuthService _cognitoAuthService;

    public SignOutHandler(ICognitoAuthService cognitoAuthService)
    {
        _cognitoAuthService = cognitoAuthService;
    }

    public async Task<Result> Handle(SignOutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _cognitoAuthService.SignOutAsync(request.AccessToken, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Sign-out failed: {ex.Message}");
        }
    }
}
