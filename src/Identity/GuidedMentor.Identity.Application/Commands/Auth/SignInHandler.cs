using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Handles user sign-in with account lockout protection.
/// - Checks if account is locked before authenticating.
/// - On failure: increments failed attempts, locks after 5 in 15 minutes.
/// - On success: resets failed attempts, issues JWT tokens.
/// </summary>
public sealed class SignInHandler : IRequestHandler<SignInCommand, Result<AuthResponse>>
{
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);
    private const int MaxFailedAttempts = 5;
    private const string GenericAuthError = "Email or password is incorrect.";

    private readonly ICognitoAuthService _cognitoAuthService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly TimeProvider _timeProvider;

    public SignInHandler(
        ICognitoAuthService cognitoAuthService,
        IUserRepository userRepository,
        IEmailNotificationService emailNotificationService,
        TimeProvider timeProvider)
    {
        _cognitoAuthService = cognitoAuthService;
        _userRepository = userRepository;
        _emailNotificationService = emailNotificationService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponse>> Handle(
        SignInCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Check lockout status
        var lockoutInfo = await _userRepository.GetLockoutInfoAsync(request.Email, cancellationToken);

        if (lockoutInfo is not null && lockoutInfo.LockedUntil.HasValue && lockoutInfo.LockedUntil.Value > now)
        {
            return Result<AuthResponse>.Failure(GenericAuthError);
        }

        // Attempt authentication via Cognito
        var result = await _cognitoAuthService.SignInAsync(request.Email, request.Password, cancellationToken);

        if (!result.IsSuccess)
        {
            await HandleFailedAttemptAsync(request.Email, lockoutInfo, now, cancellationToken);
            return Result<AuthResponse>.Failure(GenericAuthError);
        }

        // Success — reset failed attempts
        await _userRepository.ResetFailedAttemptsAsync(request.Email, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: result.AccessToken!,
            RefreshToken: result.RefreshToken!,
            IdToken: result.IdToken!,
            ActiveRole: null, // Role is determined by the caller/API layer from the user profile
            ExpiresIn: result.ExpiresIn));
    }

    private async Task HandleFailedAttemptAsync(
        string email,
        UserLockoutInfo? lockoutInfo,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _userRepository.IncrementFailedAttemptsAsync(email, cancellationToken);

        // Calculate current failed attempts within the 15-minute window
        var currentAttempts = 1;
        if (lockoutInfo is not null &&
            lockoutInfo.FirstFailedAttemptAt.HasValue &&
            now - lockoutInfo.FirstFailedAttemptAt.Value < LockoutWindow)
        {
            currentAttempts = lockoutInfo.FailedLoginAttempts + 1;
        }

        // Lock account if max attempts reached within window
        if (currentAttempts >= MaxFailedAttempts)
        {
            var lockedUntil = now.Add(LockoutDuration);
            await _userRepository.LockAccountAsync(email, lockedUntil, cancellationToken);
            await _emailNotificationService.SendAccountLockedNotificationAsync(
                email, lockedUntil, cancellationToken);
        }
    }
}
