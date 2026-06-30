using System.Security.Claims;
using System.Text.Json.Serialization;

namespace GuidedMentor.Identity.Api.Middleware;

/// <summary>
/// Endpoint filter applied to all /v1/admin/* endpoints.
/// Validates that the JWT contains a SuperAdmins group claim or custom:adminRole = true.
/// Returns 403 Forbidden if the caller is not an admin.
/// </summary>
public sealed class AdminAuthorizationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return Results.Text(
                """{"error":"Unauthorized","message":"Authentication is required."}""",
                "application/json",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!IsAdmin(user))
        {
            return Results.Text(
                """{"error":"Forbidden","message":"Admin access required. Only Super Admins can access this resource."}""",
                "application/json",
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }

    /// <summary>
    /// Checks if the user belongs to the SuperAdmins Cognito group or has custom:adminRole claim.
    /// </summary>
    private static bool IsAdmin(ClaimsPrincipal user)
    {
        // Check cognito:groups claim for SuperAdmins
        var groupsClaim = user.FindFirst("cognito:groups")?.Value;
        if (groupsClaim is not null &&
            groupsClaim.Contains("SuperAdmins", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check custom:adminRole claim
        var adminRoleClaim = user.FindFirst("custom:adminRole")?.Value;
        if (string.Equals(adminRoleClaim, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
