using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Invalidates the current access token and refresh token (global sign-out).
/// </summary>
public sealed record SignOutCommand(string AccessToken) : IRequest<Result>;
