using System.Diagnostics;
using GuidedMentor.Observability.Logging;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace GuidedMentor.Observability.Middleware;

/// <summary>
/// Middleware that propagates or generates an X-Correlation-Id header for request tracing.
/// Reads the header from the incoming request; if absent, generates a new GUID.
/// The correlation ID is stored in HttpContext.Items, added to response headers,
/// and pushed into Serilog's LogContext for structured log enrichment.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string HttpContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrGenerateCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();

        // Store in HttpContext.Items for downstream access
        context.Items[HttpContextItemKey] = correlationId;

        // Set on async-local context for enrichers
        CorrelationContext.CurrentCorrelationId = correlationId;

        // Set user ID if available from claims
        var userId = context.User?.FindFirst("sub")?.Value
                  ?? context.User?.FindFirst("cognito:username")?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            CorrelationContext.CurrentUserId = userId;
        }

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Push into Serilog LogContext for the duration of the request
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                // Log request duration as a contextual property
                using (LogContext.PushProperty("Duration", stopwatch.ElapsedMilliseconds))
                {
                    // The duration is available for any logging that occurs at this level
                }
            }
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue.ToString()))
        {
            return headerValue.ToString();
        }

        return Guid.NewGuid().ToString("D");
    }
}
