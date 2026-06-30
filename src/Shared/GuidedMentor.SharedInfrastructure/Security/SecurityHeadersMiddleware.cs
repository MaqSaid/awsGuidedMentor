using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GuidedMentor.SharedInfrastructure.Security;

/// <summary>
/// Middleware that adds security response headers to all responses.
/// Configured headers:
/// - Content-Security-Policy (CSP)
/// - X-Content-Type-Options: nosniff
/// - X-Frame-Options: DENY
/// - Strict-Transport-Security (HSTS) with max-age 31536000 (1 year)
/// - X-XSS-Protection: 0 (disabled as CSP provides better protection)
/// - Referrer-Policy: strict-origin-when-cross-origin
/// - Permissions-Policy (restrictive defaults)
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SecurityOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Set security headers before calling next to ensure they're always present
        var headers = context.Response.Headers;

        // Content Security Policy
        headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;

        // Prevent MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking
        headers["X-Frame-Options"] = "DENY";

        // HTTP Strict Transport Security (1 year, include subdomains, preload)
        headers["Strict-Transport-Security"] = $"max-age={_options.HstsMaxAgeSeconds}; includeSubDomains; preload";

        // Disable legacy XSS protection (CSP supersedes it; enabling can introduce vulnerabilities)
        headers["X-XSS-Protection"] = "0";

        // Control referrer information leakage
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Restrict browser features
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        await _next(context);
    }
}
