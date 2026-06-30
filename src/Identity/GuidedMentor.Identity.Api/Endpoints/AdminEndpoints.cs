using GuidedMentor.Identity.Api.Middleware;
using GuidedMentor.Identity.Application.Commands.Admin;
using MediatR;

namespace GuidedMentor.Identity.Api.Endpoints;

/// <summary>
/// Maps all /v1/admin/* API endpoints for Super Admin functionality.
/// All endpoints are protected by the AdminAuthorizationFilter.
/// </summary>
public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin")
            .AddEndpointFilter<AdminAuthorizationFilter>()
            .WithTags("Admin");

        group.MapGet("/dashboard", GetAdminDashboard)
            .WithName("GetAdminDashboard")
            .WithSummary("Platform health, user counts, alarm states");

        group.MapGet("/users", SearchUsers)
            .WithName("SearchUsers")
            .WithSummary("Search/filter all user accounts");

        group.MapPut("/users/{id:guid}/disable", DisableUser)
            .WithName("DisableUser")
            .WithSummary("Disable user account (with reason)");

        group.MapPut("/users/{id:guid}/enable", EnableUser)
            .WithName("EnableUser")
            .WithSummary("Re-enable user account (with reason)");

        group.MapPost("/maintenance", SetMaintenanceMode)
            .WithName("SetMaintenanceMode")
            .WithSummary("Toggle maintenance mode");

        group.MapPut("/features/{name}", ToggleFeatureFlag)
            .WithName("ToggleFeatureFlag")
            .WithSummary("Enable/disable feature flag");

        group.MapGet("/audit-log", GetAuditLog)
            .WithName("GetAuditLog")
            .WithSummary("View audit log entries");

        return group;
    }

    /// <summary>
    /// GET /v1/admin/dashboard — Retrieves admin dashboard data.
    /// </summary>
    private static async Task<IResult> GetAdminDashboard(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromClaims(httpContext);
        if (adminId is null)
            return Results.Unauthorized();

        var query = new GetAdminDashboardQuery(adminId.Value);
        var result = await mediator.Send(query, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// GET /v1/admin/users — Searches users with filters and pagination.
    /// </summary>
    private static async Task<IResult> SearchUsers(
        HttpContext httpContext,
        IMediator mediator,
        string? name,
        string? email,
        string? role,
        string? chapter,
        string? onboardingStatus,
        string? accountStatus,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var adminId = GetAdminIdFromClaims(httpContext);
        if (adminId is null)
            return Results.Unauthorized();

        var query = new SearchUsersQuery(
            adminId.Value,
            name,
            email,
            role,
            chapter,
            onboardingStatus,
            accountStatus,
            page,
            pageSize);

        var result = await mediator.Send(query, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// PUT /v1/admin/users/{id}/disable — Disables a user account.
    /// </summary>
    private static async Task<IResult> DisableUser(
        Guid id,
        DisableUserRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromClaims(httpContext);
        if (adminId is null)
            return Results.Unauthorized();

        var command = new DisableUserCommand(adminId.Value, id, request.Reason);
        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(new { message = "User account disabled successfully." })
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// PUT /v1/admin/users/{id}/enable — Re-enables a user account.
    /// </summary>
    private static async Task<IResult> EnableUser(
        Guid id,
        EnableUserRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromClaims(httpContext);
        if (adminId is null)
            return Results.Unauthorized();

        var command = new EnableUserCommand(adminId.Value, id, request.Reason);
        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(new { message = "User account re-enabled successfully." })
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// POST /v1/admin/maintenance — Toggles maintenance mode.
    /// </summary>
    private static async Task<IResult> SetMaintenanceMode(
        SetMaintenanceModeRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromClaims(httpContext);
        if (adminId is null)
            return Results.Unauthorized();

        var command = new SetMaintenanceModeCommand(
            adminId.Value,
            request.Enabled,
            request.EstimatedReturnTime,
            request.Reason);

        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(new { message = $"Maintenance mode {(request.Enabled ? "enabled" : "disabled")} successfully." })
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// PUT /v1/admin/features/{name} — Enables or disables a feature flag.
    /// </summary>
    private static async Task<IResult> ToggleFeatureFlag(
        string name,
        ToggleFeatureFlagRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromClaims(httpContext);
        if (adminId is null)
            return Results.Unauthorized();

        var command = new ToggleFeatureFlagCommand(
            adminId.Value,
            name,
            request.Enabled,
            request.Reason);

        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(new { message = $"Feature '{name}' {(request.Enabled ? "enabled" : "disabled")} successfully." })
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// GET /v1/admin/audit-log — Retrieves paginated audit log with filters.
    /// </summary>
    private static async Task<IResult> GetAuditLog(
        HttpContext httpContext,
        IMediator mediator,
        DateTime? startDate,
        DateTime? endDate,
        string? actionType,
        Guid? filterAdminId,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var adminId = GetAdminIdFromClaims(httpContext);
        if (adminId is null)
            return Results.Unauthorized();

        var query = new GetAuditLogQuery(
            adminId.Value,
            startDate,
            endDate,
            actionType,
            filterAdminId,
            page,
            pageSize);

        var result = await mediator.Send(query, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Extracts the admin user ID from the JWT claims (sub claim from Cognito).
    /// </summary>
    private static Guid? GetAdminIdFromClaims(HttpContext httpContext)
    {
        var subClaim = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (subClaim is null || !Guid.TryParse(subClaim, out var adminId))
        {
            return null;
        }

        return adminId;
    }
}

// Request DTOs for admin endpoints

/// <summary>
/// Request body for disabling a user account.
/// </summary>
public sealed record DisableUserRequest(string Reason);

/// <summary>
/// Request body for enabling a user account.
/// </summary>
public sealed record EnableUserRequest(string Reason);

/// <summary>
/// Request body for setting maintenance mode.
/// </summary>
public sealed record SetMaintenanceModeRequest(
    bool Enabled,
    string? EstimatedReturnTime,
    string Reason);

/// <summary>
/// Request body for toggling a feature flag.
/// </summary>
public sealed record ToggleFeatureFlagRequest(bool Enabled, string Reason);
