using MediatR;
using GuidedMentor.Mentoring.Application.Commands.Sessions;
using System.Security.Claims;

namespace GuidedMentor.Mentoring.Api.Endpoints;

/// <summary>
/// Maps session management endpoints for the Mentoring bounded context.
/// Handles: GET /v1/sessions, POST /v1/sessions/{id}/accept, POST /v1/sessions/{id}/decline,
///           POST /v1/sessions/{id}/complete
/// </summary>
public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/sessions")
            .WithTags("Sessions")
            .RequireAuthorization();

        // GET /v1/sessions — List sessions for the authenticated user (active, pending, completed)
        group.MapGet("/", async (
            HttpContext httpContext,
            IMediator mediator,
            string? status = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var query = new GetSessionsQuery(userId.Value, status, page, pageSize);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        });

        // GET /v1/sessions/{sessionId} — Get session detail
        group.MapGet("/{sessionId:guid}", async (
            Guid sessionId,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var query = new GetSessionDetailQuery(userId.Value, sessionId);
            var result = await mediator.Send(query, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        });

        // POST /v1/sessions/{sessionId}/accept — Mentor accepts a pending session request
        group.MapPost("/{sessionId:guid}/accept", async (
            Guid sessionId,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var command = new AcceptRequestCommand(userId.Value, sessionId);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(new { Status = "active", SessionId = sessionId })
                : Results.BadRequest(new { Error = result.Error });
        });

        // POST /v1/sessions/{sessionId}/decline — Mentor declines a pending session request
        group.MapPost("/{sessionId:guid}/decline", async (
            Guid sessionId,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var command = new DeclineRequestCommand(userId.Value, sessionId);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { Error = result.Error });
        });

        // POST /v1/sessions/{sessionId}/complete — Mark session as complete (mentee first, then mentor confirms)
        group.MapPost("/{sessionId:guid}/complete", async (
            Guid sessionId,
            MarkCompleteRequest request,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            if (!Enum.TryParse<GuidedMentor.SharedKernel.Role>(request.Role, ignoreCase: true, out var role))
                return Results.BadRequest(new { Error = "Invalid role. Must be 'Mentor' or 'Mentee'." });

            var command = new MarkCompleteCommand(sessionId, userId.Value, role);
            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(new { Status = "completed" })
                : Results.BadRequest(new { Error = result.Error });
        });
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}

// Query records dispatched via MediatR
public sealed record GetSessionsQuery(
    Guid UserId,
    string? StatusFilter,
    int Page,
    int PageSize) : IRequest<SessionListResult>;

public sealed record GetSessionDetailQuery(
    Guid UserId,
    Guid SessionId) : IRequest<SessionDetailDto?>;

public sealed record SessionListResult(
    IReadOnlyList<SessionSummaryDto> Sessions,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record SessionSummaryDto(
    Guid SessionId,
    string Status,
    string PartnerName,
    string PartnerRole,
    int ProgressPercent,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record SessionDetailDto(
    Guid SessionId,
    string Status,
    Guid MentorId,
    string MentorName,
    Guid MenteeId,
    string MenteeName,
    int ProgressPercent,
    DateTime CreatedAt,
    DateTime? MenteeCompletedAt,
    DateTime? MentorCompletedAt,
    DateTime? CompletedAt,
    bool HasSessionPlan);

public sealed record MarkCompleteRequest(string Role);
