using GuidedMentor.Identity.Application.DTOs;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Exchanges a Google OAuth authorization code for Cognito tokens and creates/retrieves the user record.
/// </summary>
public sealed record GoogleOAuthSignupCommand(string AuthorizationCode) : IRequest<AuthResponse>;
