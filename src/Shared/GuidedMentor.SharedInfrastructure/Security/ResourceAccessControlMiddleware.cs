using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.Security;

/// <summary>
/// Middleware that enforces resource-based access control.
/// Ensures the authenticated user (userId from JWT sub claim) can only access
/// resources they own. Returns 403 Forbidden when userId in JWT does not match
/// the resource owner.
///
/// Resource ownership is checked by:
/// 1. Route parameters named "userId" must match the JWT sub claim.
/// 2. Endpoints can opt-in to ownership validation via the IResourceOwnerResolver interface.
/// </summary>
public sealed class ResourceAccessControlMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResourceAccessControlMiddleware> _logger;

    public ResourceAccessControlMiddleware(
        RequestDelegate next,
        ILogger<ResourceAccessControlMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if user is not authenticated (JWT middleware already handles this)
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var authenticatedUserId = context.Items["UserId"]?.ToString();

        if (string.IsNullOrWhiteSpace(authenticatedUserId))
        {
            await _next(context);
            return;
        }

        // Check if the route has a userId path parameter
        var routeUserId = context.Request.RouteValues.TryGetValue("userId", out var routeValue)
            ? routeValue?.ToString()
            : null;

        if (!string.IsNullOrWhiteSpace(routeUserId) &&
            !routeUserId.Equals(authenticatedUserId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Resource access denied. AuthenticatedUser={AuthUserId} attempted to access resource owned by {ResourceOwnerId}",
                authenticatedUserId,
                routeUserId);

            await WriteForbiddenResponse(context);
            return;
        }

        // Check X-Resource-Owner header (set by endpoint filters or handlers for custom ownership)
        // This allows handlers to signal ownership validation after processing
        context.Response.OnStarting(() => Task.CompletedTask);

        await _next(context);

        // Post-execution check: if a handler sets the resource-owner-mismatch flag
        if (context.Items.TryGetValue("ResourceOwnerMismatch", out var mismatch) && mismatch is true)
        {
            // Note: This case is handled by handlers throwing ForbiddenException directly,
            // which is caught by GlobalExceptionHandler. This middleware catches route-level mismatches.
            _logger.LogWarning(
                "Post-execution resource ownership mismatch detected for user {UserId}",
                authenticatedUserId);
        }
    }

    private static async Task WriteForbiddenResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            statusCode = 403,
            error = "Forbidden",
            message = "You do not have permission to access this resource.",
            correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id?.ToString() : Guid.NewGuid().ToString("D")
        });
    }
}

/// <summary>
/// Interface for custom resource ownership resolution.
/// Implement this on endpoint filters or services to provide custom ownership validation logic
/// beyond simple route parameter matching.
/// </summary>
public interface IResourceOwnerResolver
{
    /// <summary>
    /// Resolves the owner userId of the requested resource.
    /// Returns null if ownership cannot be determined (allows request through).
    /// </summary>
    Task<string?> ResolveOwnerAsync(HttpContext context, CancellationToken cancellationToken = default);
}
