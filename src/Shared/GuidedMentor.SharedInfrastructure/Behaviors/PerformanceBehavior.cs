using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs a warning if a handler takes longer than 500ms.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMs = 500;

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
        {
            _logger.LogWarning(
                "Long running request: {RequestName} ({ElapsedMilliseconds}ms) {@Request}",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds,
                request);
        }

        return response;
    }
}
