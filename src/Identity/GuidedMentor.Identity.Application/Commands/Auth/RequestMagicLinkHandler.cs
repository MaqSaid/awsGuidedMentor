using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Identity.Application.Commands.Auth;

/// <summary>
/// Handles magic link request: validates email, checks rate limit, initiates send.
/// </summary>
public sealed class RequestMagicLinkHandler : IRequestHandler<RequestMagicLinkCommand, Result>
{
    private readonly IMagicLinkService _magicLinkService;
    private readonly ILogger<RequestMagicLinkHandler> _logger;

    public RequestMagicLinkHandler(IMagicLinkService magicLinkService, ILogger<RequestMagicLinkHandler> logger)
    {
        _magicLinkService = magicLinkService;
        _logger = logger;
    }

    public async Task<Result> Handle(RequestMagicLinkCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return Result.Failure("Please enter a valid email address.");
        }

        // Rate limit: max 3 requests per email per 15 minutes
        if (!await _magicLinkService.CanSendAsync(request.Email, cancellationToken))
        {
            return Result.Failure("Too many sign-in requests. Please wait a few minutes and try again.");
        }

        await _magicLinkService.SendMagicLinkAsync(request.Email, cancellationToken);

        _logger.LogInformation("Magic link sent to {Email}", request.Email);

        return Result.Success();
    }
}
