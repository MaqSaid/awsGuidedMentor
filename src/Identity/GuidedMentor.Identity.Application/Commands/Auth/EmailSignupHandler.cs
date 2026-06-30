using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Handles email/password signup by creating the user in Cognito (pending state)
/// and sending a verification email with a 10-minute expiry code.
/// </summary>
public sealed class EmailSignupHandler : IRequestHandler<EmailSignupCommand, Result<EmailSignupResponse>>
{
    private readonly ICognitoAuthService _cognitoAuthService;

    public EmailSignupHandler(ICognitoAuthService cognitoAuthService)
    {
        _cognitoAuthService = cognitoAuthService;
    }

    public async Task<Result<EmailSignupResponse>> Handle(
        EmailSignupCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _cognitoAuthService.SignUpWithEmailAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Result<EmailSignupResponse>.Failure(
                result.ErrorMessage ?? "Sign-up failed.");
        }

        return Result<EmailSignupResponse>.Success(
            new EmailSignupResponse(
                Message: "Verification code sent. Please check your email.",
                UserId: result.UserId!));
    }
}
