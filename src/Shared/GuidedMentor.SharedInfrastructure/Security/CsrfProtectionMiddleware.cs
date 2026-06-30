using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuidedMentor.SharedInfrastructure.Security;

/// <summary>
/// Middleware that provides CSRF protection via SameSite=Strict cookie enforcement
/// and Origin header validation on state-changing requests (POST, PUT, PATCH, DELETE).
///
/// Strategy:
/// 1. Sets SameSite=Strict on all cookies (prevents cross-site cookie inclusion).
/// 2. Validates Origin header on state-changing requests matches allowed origins.
/// 3. Falls back to Referer header validation if Origin is absent.
///
/// GET/HEAD/OPTIONS requests are exempt (they should not modify state).
/// </summary>
public sealed class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;

    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options
    };

    public CsrfProtectionMiddleware(
        RequestDelegate next,
        IOptions<SecurityOptions> options,
        ILogger<CsrfProtectionMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Enforce SameSite=Strict on all response cookies
        context.Response.OnStarting(() =>
        {
            EnforceSameSiteStrict(context);
            return Task.CompletedTask;
        });

        // Only validate origin on state-changing methods
        if (SafeMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Skip CSRF check for anonymous paths (login, signup, etc.)
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsAnonymousPath(path))
        {
            await _next(context);
            return;
        }

        // Validate Origin or Referer header
        if (!IsValidOrigin(context))
        {
            _logger.LogWarning(
                "CSRF validation failed. Method={Method} Path={Path} Origin={Origin} Referer={Referer}",
                context.Request.Method,
                path,
                context.Request.Headers.Origin.ToString(),
                context.Request.Headers.Referer.ToString());

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 403,
                error = "CsrfValidationFailed",
                message = "Request origin validation failed.",
                correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id?.ToString() : Guid.NewGuid().ToString("D")
            });
            return;
        }

        await _next(context);
    }

    private bool IsValidOrigin(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();

        // If Origin header is present, validate it
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return _options.AllowedOrigins.Any(allowed =>
                origin.Equals(allowed, StringComparison.OrdinalIgnoreCase));
        }

        // Fallback: check Referer header
        var referer = context.Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
            var refererOrigin = $"{refererUri.Scheme}://{refererUri.Host}";
            if (refererUri.Port != 80 && refererUri.Port != 443)
            {
                refererOrigin += $":{refererUri.Port}";
            }

            return _options.AllowedOrigins.Any(allowed =>
                refererOrigin.Equals(allowed, StringComparison.OrdinalIgnoreCase));
        }

        // If neither Origin nor Referer is present, allow the request
        // (same-origin requests from some browsers may not include these headers)
        // The JWT validation layer provides the primary authentication gate.
        return true;
    }

    private bool IsAnonymousPath(string path)
    {
        return _options.AnonymousPaths.Any(p =>
            path.Equals(p, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static void EnforceSameSiteStrict(HttpContext context)
    {
        var setCookieHeaders = context.Response.Headers.SetCookie;
        if (setCookieHeaders.Count == 0) return;

        var updatedCookies = new List<string>();

        foreach (var cookie in setCookieHeaders)
        {
            if (string.IsNullOrWhiteSpace(cookie)) continue;

            var updatedCookie = cookie;

            // Replace any existing SameSite directive with Strict
            if (cookie.Contains("SameSite=", StringComparison.OrdinalIgnoreCase))
            {
                updatedCookie = System.Text.RegularExpressions.Regex.Replace(
                    cookie,
                    @"SameSite=\w+",
                    "SameSite=Strict",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            else
            {
                // Add SameSite=Strict if not present
                updatedCookie += "; SameSite=Strict";
            }

            // Ensure Secure flag is present
            if (!updatedCookie.Contains("Secure", StringComparison.OrdinalIgnoreCase))
            {
                updatedCookie += "; Secure";
            }

            updatedCookies.Add(updatedCookie);
        }

        context.Response.Headers.SetCookie = new Microsoft.Extensions.Primitives.StringValues(updatedCookies.ToArray());
    }
}
