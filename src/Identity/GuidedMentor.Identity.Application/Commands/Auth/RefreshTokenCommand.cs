using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Exchanges a refresh token for a new access token without requiring re-authentication.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;
