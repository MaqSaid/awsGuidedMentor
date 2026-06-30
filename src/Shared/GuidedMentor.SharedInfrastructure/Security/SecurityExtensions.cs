using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GuidedMentor.SharedInfrastructure.Security;

/// <summary>
/// Extension methods for registering security services and middleware.
/// Provides a single entry point for all security concerns:
/// - JWT validation (Cognito authorizer, 15-min access token)
/// - Resource-based access control (userId match)
/// - Rate limiting (100 req/min/user, sliding window)
/// - Request body size limit (1 MB)
/// - Security response headers (CSP, X-Content-Type-Options, X-Frame-Options, HSTS)
/// - CSRF protection (SameSite=Strict + Origin validation)
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Registers security options from configuration.
    /// Call this in the service registration phase (builder.Services).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGuidedMentorSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SecurityOptions>(
            configuration.GetSection(SecurityOptions.SectionName));

        // Register options as a singleton for direct injection
        services.AddSingleton(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SecurityOptions>>().Value);

        return services;
    }

    /// <summary>
    /// Adds all security middleware to the request pipeline in the correct order.
    /// Call after UseRouting() and before MapEndpoints().
    ///
    /// Middleware order:
    /// 1. Security headers (runs on all responses)
    /// 2. Request body size limit (early rejection of oversized payloads)
    /// 3. CSRF protection (Origin validation for state-changing requests)
    /// 4. JWT validation (authentication gate)
    /// 5. Rate limiting (per-user throttling, requires authenticated userId)
    /// 6. Resource access control (authorization check, requires authenticated userId)
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseGuidedMentorSecurity(this WebApplication app)
    {
        // 1. Security headers — always first, applies to all responses
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // 2. Request body size limit — reject oversized payloads early
        app.UseMiddleware<RequestBodySizeLimitMiddleware>();

        // 3. CSRF protection — validate origin before processing
        app.UseMiddleware<CsrfProtectionMiddleware>();

        // 4. JWT validation — authenticate the user
        app.UseMiddleware<JwtValidationMiddleware>();

        // 5. Rate limiting — throttle after authentication (needs userId)
        app.UseMiddleware<RateLimitingMiddleware>();

        // 6. Resource access control — authorize after authentication
        app.UseMiddleware<ResourceAccessControlMiddleware>();

        return app;
    }
}
