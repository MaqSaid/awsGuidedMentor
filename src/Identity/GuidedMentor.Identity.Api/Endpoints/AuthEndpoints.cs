using System.Security.Claims;
using System.Text.Json;
using GuidedMentor.Identity.Application.Commands.Auth;
using GuidedMentor.Identity.Application.Commands.Onboarding;
using GuidedMentor.Identity.Application.Commands.RoleSelection;
using GuidedMentor.Identity.Application.Commands.Settings;
using GuidedMentor.Identity.Application.Commands.Upload;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Api.Endpoints;

/// <summary>
/// Maps authentication, role management, onboarding, and file upload endpoints.
/// These are the core Identity context endpoints for user-facing operations.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // === Authentication (no auth required) ===
        var auth = app.MapGroup("/v1/auth")
            .WithTags("Authentication");

        auth.MapPost("/signup", async (SignupRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new EmailSignupCommand(request.Email, request.Password);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { message = "Verification code sent to your email." })
                : Results.BadRequest(new { error = result.Error });
        });

        auth.MapPost("/verify", async (VerifyEmailRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new VerifyEmailCommand(request.Email, request.Code);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { message = "Email verified successfully." })
                : Results.BadRequest(new { error = result.Error });
        });

        auth.MapPost("/signin", async (SignInRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new SignInCommand(request.Email, request.Password);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Unauthorized();
        });

        auth.MapPost("/google", async (GoogleOAuthRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new GoogleOAuthSignupCommand(request.Code);
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        });

        auth.MapPost("/refresh", async (RefreshTokenRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var command = new RefreshTokenCommand(request.RefreshToken);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Unauthorized();
        });

        auth.MapPost("/signout", async (HttpContext httpContext, IMediator mediator, CancellationToken ct) =>
        {
            var accessToken = httpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(accessToken)) return Results.Unauthorized();

            var command = new SignOutCommand(accessToken);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization();

        // === Role Management (JWT required) ===
        var role = app.MapGroup("/v1/role")
            .WithTags("Role")
            .RequireAuthorization();

        role.MapPost("/select", async (SelectRoleRequest request, HttpContext httpContext, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            if (!Enum.TryParse<Role>(request.Role, ignoreCase: true, out var parsedRole))
                return Results.BadRequest(new { error = "Invalid role. Must be 'Mentor' or 'Mentee'." });

            var command = new SetRoleCommand(userId.Value, parsedRole);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { activeRole = request.Role, redirectTo = "/onboarding" })
                : Results.BadRequest(new { error = result.Error });
        });

        role.MapPost("/toggle", async (HttpContext httpContext, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var command = new ToggleRoleCommand(userId.Value);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { activeRole = result.Value })
                : Results.BadRequest(new { error = result.Error });
        });

        // === Onboarding (JWT required) ===
        var onboarding = app.MapGroup("/v1/onboarding")
            .WithTags("Onboarding")
            .RequireAuthorization();

        onboarding.MapPost("/step", async (HttpContext httpContext, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var jsonDoc = await JsonDocument.ParseAsync(httpContext.Request.Body, cancellationToken: ct);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("role", out var roleElement) ||
                !Enum.TryParse<Role>(roleElement.GetString(), ignoreCase: true, out var parsedRole))
                return Results.BadRequest(new { error = "Invalid role." });

            if (!root.TryGetProperty("step", out var stepElement) || !stepElement.TryGetInt32(out var step))
                return Results.BadRequest(new { error = "Invalid step." });

            JsonDocument? dataDoc = null;
            if (root.TryGetProperty("data", out var dataElement))
            {
                dataDoc = JsonDocument.Parse(dataElement.GetRawText());
            }

            var command = new SaveOnboardingStepCommand(userId.Value, parsedRole, step, dataDoc ?? JsonDocument.Parse("{}"));
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { step, status = "saved" })
                : Results.BadRequest(new { error = result.Error });
        });

        onboarding.MapGet("/progress", async (HttpContext httpContext, IMediator mediator, string role, CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            if (!Enum.TryParse<Role>(role, ignoreCase: true, out var parsedRole))
                return Results.BadRequest(new { error = "Invalid role." });

            var query = new GetOnboardingProgressQuery(userId.Value, parsedRole);
            var result = await mediator.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        // === File Upload (JWT required) ===
        var files = app.MapGroup("/v1/files")
            .WithTags("Files")
            .RequireAuthorization();

        files.MapPost("/upload-url", async (GetPresignedUrlRequest request, HttpContext httpContext, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            if (!Enum.TryParse<UploadType>(request.Purpose, ignoreCase: true, out var uploadType))
                return Results.BadRequest(new { error = "Invalid upload purpose. Must be 'Resume' or 'ProfilePhoto'." });

            var command = new GetUploadUrlCommand(userId.Value, request.FileName, request.FileType, request.FileSize, uploadType);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { uploadUrl = result.Value.UploadUrl, expiresIn = "5 minutes" })
                : Results.BadRequest(new { error = result.Error });
        });

        files.MapPost("/download-url", async (GetDownloadUrlRequest request, HttpContext httpContext, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            if (!Enum.TryParse<UploadType>(request.FileType, ignoreCase: true, out var uploadType))
                return Results.BadRequest(new { error = "Invalid file type." });

            var query = new GetDownloadUrlQuery(userId.Value, request.TargetUserId, uploadType);
            var result = await mediator.Send(query, ct);
            return result.IsSuccess
                ? Results.Ok(new { downloadUrl = result.Value.DownloadUrl, expiresIn = "15 minutes" })
                : Results.BadRequest(new { error = result.Error });
        });

        // === Settings (JWT required) ===
        var settings = app.MapGroup("/v1/settings")
            .WithTags("Settings")
            .RequireAuthorization();

        settings.MapPut("/", async (HttpContext httpContext, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var jsonDoc = await JsonDocument.ParseAsync(httpContext.Request.Body, cancellationToken: ct);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("role", out var roleElement) ||
                !Enum.TryParse<Role>(roleElement.GetString(), ignoreCase: true, out var parsedRole))
                return Results.BadRequest(new { error = "Invalid role." });

            JsonDocument dataDoc;
            if (root.TryGetProperty("data", out var dataElement))
            {
                dataDoc = JsonDocument.Parse(dataElement.GetRawText());
            }
            else
            {
                return Results.BadRequest(new { error = "Missing data field." });
            }

            var command = new UpdateSettingsCommand(userId.Value, parsedRole, dataDoc);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { message = "Settings updated." })
                : Results.BadRequest(new { error = result.Error });
        });
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}

// Request DTOs
public sealed record SignupRequest(string Email, string Password);
public sealed record VerifyEmailRequest(string Email, string Code);
public sealed record SignInRequest(string Email, string Password);
public sealed record GoogleOAuthRequest(string Code);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record SelectRoleRequest(string Role);
public sealed record GetPresignedUrlRequest(string FileName, string FileType, string Purpose, long FileSize);
public sealed record GetDownloadUrlRequest(Guid TargetUserId, string FileType);
