using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Signs up a new user with email and password.
/// Creates the user in a pending state and sends a verification code (10-min expiry).
/// </summary>
public sealed record EmailSignupCommand(string Email, string Password) : IRequest<Result<EmailSignupResponse>>;

/// <summary>
/// Response returned on successful email signup.
/// </summary>
public sealed record EmailSignupResponse(string Message, string UserId);
