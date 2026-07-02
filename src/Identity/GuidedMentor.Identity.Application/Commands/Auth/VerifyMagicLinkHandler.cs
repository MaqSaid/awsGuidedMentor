using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Handles magic link verification: validates token, exchanges for JWT tokens.
/// </summary>
public sealed class VerifyMagicLinkHandler : IRequestHandler<VerifyMagicLinkCommand, Result<AuthResponse>>
{
    private readonly IMagicLinkService _magicLinkService;
    private readonly ILogger<VerifyMagicLinkHandler> _logger;

    public VerifyMagicLinkHandler(IMagicLinkService magicLinkService, ILogger<VerifyMagicLinkHandler> logger)
    {
        _magicLinkService = magicLinkService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(VerifyMagicLinkCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result<AuthResponse>.Failure("Invalid or missing token.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<AuthResponse>.Failure("Invalid or missing email.");

        var result = await _magicLinkService.VerifyAndAuthenticateAsync(request.Email, request.Token, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Magic link verification failed for {Email}", request.Email);
            return Result<AuthResponse>.Failure(result.Error);
        }

        _logger.LogInformation("Magic link verified for {Email}", request.Email);
        return result;
    }
}
