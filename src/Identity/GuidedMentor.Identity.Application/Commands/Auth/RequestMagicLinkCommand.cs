using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Initiates a magic link sign-in by calling Cognito InitiateAuth with CUSTOM_AUTH.
/// </summary>
public sealed record RequestMagicLinkCommand(string Email) : IRequest<Result>;
