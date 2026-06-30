using MediatR;
using GuidedMentor.Mentoring.Application.Commands.Locking;
using GuidedMentor.Mentoring.Application.DTOs;
using System.Security.Claims;

namespace GuidedMentor.Mentoring.Api.Endpoints;

/// <summary>
/// Maps locking mechanism endpoints for the Mentoring bounded context.
/// Handles: POST /v1/locks (acquire), DELETE /v1/locks/{id} (release), POST /v1/locks/{id}/confirm (confirm selection)
/// </summary>
public static class LockingEndpoints
{
    public static void MapLockingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/locks")
            .WithTags("Locking")
            .RequireAuthorization();

        // POST /v1/locks — Acquire a 15-minute lock on a mentor
        group.MapPost("/", async (
            AcquireLockRequest request,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var command = new AcquireLockCommand(userId.Value, request.MentorId);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Created($"/v1/locks/{result.Value.LockId}", result.Value)
                : Results.Conflict(new ErrorResponse(result.Error));
        });

        // DELETE /v1/locks/{lockId} — Release an active lock
        group.MapDelete("/{lockId:guid}", async (
            Guid lockId,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var command = new ReleaseLockCommand(userId.Value, lockId);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new ErrorResponse(result.Error));
        });

        // POST /v1/locks/{lockId}/confirm — Confirm mentor selection (creates pending session)
        group.MapPost("/{lockId:guid}/confirm", async (
            Guid lockId,
            ConfirmSelectionRequest request,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var command = new ConfirmSelectionCommand(lockId, userId.Value, request.MentorId);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new ErrorResponse(result.Error));
        });
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}

internal sealed record AcquireLockRequest(Guid MentorId);
internal sealed record ConfirmSelectionRequest(Guid MentorId);
internal sealed record ErrorResponse(string Error);
