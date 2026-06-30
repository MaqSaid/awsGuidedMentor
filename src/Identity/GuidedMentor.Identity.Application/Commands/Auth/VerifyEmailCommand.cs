using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Verifies a user's email using the 6-digit code sent during signup.
/// Validates code within 10-minute expiry and maximum 5 attempts.
/// </summary>
public sealed record VerifyEmailCommand(string Email, string Code) : IRequest<Result<VerifyEmailResponse>>;

/// <summary>
/// Response returned from email verification.
/// </summary>
public sealed record VerifyEmailResponse(bool Verified);
