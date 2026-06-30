using MediatR;
using GuidedMentor.Mentoring.Application.Commands.Locking;
using GuidedMentor.Mentoring.Application.DTOs;
using System.Security.Claims;

namespace GuidedMentor.Mentoring.Api.Endpoints;

/// <summary>
/// Maps browse mentor endpoints for the Mentoring bounded context.
/// Handles: GET /v1/mentors (browse), GET /v1/mentors/{id} (detail)
/// </summary>
public static class BrowseEndpoints
{
    public static void MapBrowseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/mentors")
            .WithTags("Browse")
            .RequireAuthorization();

        // GET /v1/mentors — Browse available mentors with compatibility scoring
        group.MapGet("/", async (
            HttpContext httpContext,
            IMediator mediator,
            int page = 1,
            int pageSize = 12,
            string? chapter = null,
            string? skills = null,
            CancellationToken ct = default) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            // BrowseMentorsQuery is dispatched via MediatR
            var query = new BrowseMentorsQuery(
                userId.Value,
                page,
                pageSize,
                chapter,
                skills?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("BrowseMentors")
        .WithDescription("Browse available mentors with optional filtering and compatibility scoring.")
        .Produces<BrowseMentorsResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // GET /v1/mentors/{mentorId} — Get mentor detail with compatibility score
        group.MapGet("/{mentorId:guid}", async (
            Guid mentorId,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null) return Results.Unauthorized();

            var query = new GetMentorDetailQuery(userId.Value, mentorId);
            var result = await mediator.Send(query, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetMentorDetail")
        .WithDescription("Retrieves detailed mentor profile including compatibility score.")
        .Produces<MentorDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized);
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}

// Query records dispatched via MediatR (handlers exist in Application layer)
public sealed record BrowseMentorsQuery(
    Guid MenteeUserId,
    int Page,
    int PageSize,
    string? Chapter,
    string[]? Skills) : IRequest<BrowseMentorsResult>;

public sealed record GetMentorDetailQuery(
    Guid MenteeUserId,
    Guid MentorId) : IRequest<MentorDetailDto?>;

public sealed record BrowseMentorsResult(
    IReadOnlyList<MentorCardDto> Mentors,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record MentorCardDto(
    Guid MentorId,
    string DisplayName,
    string Title,
    string Chapter,
    IReadOnlyList<string> ExpertiseAreas,
    int CompatibilityScore,
    bool HasActiveOpportunities,
    string AvailabilityStatus);

public sealed record MentorDetailDto(
    Guid MentorId,
    string DisplayName,
    string Title,
    string Company,
    string Chapter,
    string City,
    string Bio,
    IReadOnlyList<string> ExpertiseAreas,
    IReadOnlyList<string> Certifications,
    int CompatibilityScore,
    int ActiveMenteeCount,
    int MaxMentees,
    bool HasActiveOpportunities,
    string AvailabilityStatus);
