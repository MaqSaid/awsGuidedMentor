using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Verifies the magic link token and completes authentication.
/// Returns JWT tokens on success.
/// </summary>
public sealed record VerifyMagicLinkCommand(string Email, string Token) : IRequest<Result<AuthResponse>>;
