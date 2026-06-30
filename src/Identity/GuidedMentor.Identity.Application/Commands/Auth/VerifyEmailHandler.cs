using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Handles email verification by validating the code within the 10-minute expiry
/// and 5-attempt limit, then activating the account.
/// </summary>
public sealed class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    private readonly ICognitoAuthService _cognitoAuthService;

    public VerifyEmailHandler(ICognitoAuthService cognitoAuthService)
    {
        _cognitoAuthService = cognitoAuthService;
    }

    public async Task<Result<VerifyEmailResponse>> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _cognitoAuthService.VerifyEmailAsync(
            request.Email,
            request.Code,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Result<VerifyEmailResponse>.Failure(
                result.ErrorMessage ?? "Verification failed.");
        }

        return Result<VerifyEmailResponse>.Success(new VerifyEmailResponse(Verified: true));
    }
}
