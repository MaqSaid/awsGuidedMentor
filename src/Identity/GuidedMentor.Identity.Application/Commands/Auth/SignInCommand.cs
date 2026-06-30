using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Authenticates a user with email and password, issues JWT tokens.
/// Implements account lockout: 5 failures in 15 minutes → 30-minute lock + email notification.
/// </summary>
public sealed record SignInCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
