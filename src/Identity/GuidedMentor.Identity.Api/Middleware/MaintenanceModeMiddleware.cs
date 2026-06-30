using System.Text.Json;
using System.Text.Json.Serialization;
using GuidedMentor.Identity.Application.Interfaces;

namespace GuidedMentor.Identity.Api.Middleware;

/// <summary>
/// Middleware that checks if the platform is in maintenance mode on every request.
/// If enabled and the request is NOT from an admin (SuperAdmins Cognito group claim),
/// returns 503 with a JSON body indicating the platform is temporarily unavailable.
/// Admin requests bypass the check entirely.
/// </summary>
public sealed class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IFeatureFlagService featureFlagService)
    {
        // Check if the current request is from an admin user
        if (IsAdminRequest(context))
        {
            await _next(context);
            return;
        }

        // Check maintenance mode
        var isMaintenanceMode = await featureFlagService.IsMaintenanceModeEnabledAsync(context.RequestAborted);

        if (isMaintenanceMode)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";

            var response = new MaintenanceResponse
            {
                Message = "Platform temporarily unavailable",
                EstimatedReturnTime = null
            };

            await context.Response.WriteAsJsonAsync(
                response,
                MaintenanceJsonContext.Default.MaintenanceResponse,
                cancellationToken: context.RequestAborted);

            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Determines if the request is from an admin by checking JWT claims
    /// for the SuperAdmins Cognito group or the custom:adminRole claim.
    /// </summary>
    private static bool IsAdminRequest(HttpContext context)
    {
        var user = context.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Check for cognito:groups claim containing "SuperAdmins"
        var groupsClaim = user.FindFirst("cognito:groups")?.Value;
        if (groupsClaim is not null &&
            groupsClaim.Contains("SuperAdmins", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for custom:adminRole claim
        var adminRoleClaim = user.FindFirst("custom:adminRole")?.Value;
        if (string.Equals(adminRoleClaim, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Response model for maintenance mode 503 response.
/// </summary>
public sealed class MaintenanceResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("estimatedReturnTime")]
    public string? EstimatedReturnTime { get; set; }
}

/// <summary>
/// AOT-compatible JSON source generator context for maintenance responses.
/// </summary>
[JsonSerializable(typeof(MaintenanceResponse))]
internal sealed partial class MaintenanceJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Extension methods for registering MaintenanceModeMiddleware.
/// </summary>
public static class MaintenanceModeMiddlewareExtensions
{
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MaintenanceModeMiddleware>();
    }
}
