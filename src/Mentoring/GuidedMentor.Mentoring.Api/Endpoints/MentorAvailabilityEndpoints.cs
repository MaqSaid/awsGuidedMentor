using GuidedMentor.Mentoring.Application.Commands.Availability;
using GuidedMentor.Mentoring.Application.Queries.Availability;
using GuidedMentor.Mentoring.Domain.ValueObjects;
using MediatR;

namespace GuidedMentor.Mentoring.Api.Endpoints;

/// <summary>
/// Maps /v1/mentors/me/availability endpoints for the mentor availability toggle feature.
/// </summary>
public static class MentorAvailabilityEndpoints
{
    public static RouteGroupBuilder MapMentorAvailabilityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/mentors/me/availability")
            .WithTags("Mentor Availability");

        group.MapPut("/", SetAvailability)
            .WithName("SetMentorAvailability")
            .WithSummary("Toggle mentor availability status (available/unavailable)");

        group.MapGet("/", GetAvailability)
            .WithName("GetMentorAvailability")
            .WithSummary("Get current mentor availability status");

        return group;
    }

    /// <summary>
    /// PUT /v1/mentors/me/availability — Sets the mentor's availability status.
    /// </summary>
    private static async Task<IResult> SetAvailability(
        SetAvailabilityRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var mentorId = GetMentorIdFromClaims(httpContext);
        if (mentorId is null)
            return Results.Unauthorized();

        if (!Enum.TryParse<AvailabilityStatus>(request.Status, ignoreCase: true, out var status))
        {
            return Results.BadRequest(new { error = "Invalid status. Must be 'available' or 'unavailable'." });
        }

        UnavailabilityReason? reason = null;
        if (request.Reason is not null)
        {
            if (!Enum.TryParse<UnavailabilityReason>(request.Reason, ignoreCase: true, out var parsedReason))
            {
                return Results.BadRequest(new { error = "Invalid reason. Must be 'vacation', 'personalcommitment', 'workload', or 'other'." });
            }
            reason = parsedReason;
        }

        var command = new SetMentorAvailabilityCommand(
            mentorId.Value,
            status,
            reason,
            request.ReturnDate);

        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(new { message = $"Availability set to {status.ToString().ToLowerInvariant()} successfully." })
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// GET /v1/mentors/me/availability — Gets the mentor's current availability status.
    /// </summary>
    private static async Task<IResult> GetAvailability(
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var mentorId = GetMentorIdFromClaims(httpContext);
        if (mentorId is null)
            return Results.Unauthorized();

        var query = new GetMentorAvailabilityQuery(mentorId.Value);
        var result = await mediator.Send(query, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Extracts the mentor user ID from the JWT claims (sub claim from Cognito).
    /// </summary>
    private static Guid? GetMentorIdFromClaims(HttpContext httpContext)
    {
        var subClaim = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (subClaim is null || !Guid.TryParse(subClaim, out var mentorId))
        {
            return null;
        }

        return mentorId;
    }
}

/// <summary>
/// Request body for setting mentor availability.
/// </summary>
public sealed record SetAvailabilityRequest(
    string Status,
    string? Reason = null,
    DateTime? ReturnDate = null);
