using GuidedMentor.Mentoring.Application.Commands.Opportunities;
using GuidedMentor.Mentoring.Domain.Entities;
using MediatR;

namespace GuidedMentor.Mentoring.Api.Endpoints;

/// <summary>
/// Maps /v1/users/me/opportunity-preferences endpoints for mentee notification preferences.
/// </summary>
public static class OpportunityPreferencesEndpoints
{
    public static RouteGroupBuilder MapOpportunityPreferencesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/users/me/opportunity-preferences")
            .WithTags("Opportunity Preferences");

        group.MapPut("/", UpdatePreferences)
            .WithName("UpdateOpportunityPreferences")
            .WithSummary("Update mentee opportunity notification preferences");

        return group;
    }

    /// <summary>
    /// PUT /v1/users/me/opportunity-preferences — Updates mentee notification preferences.
    /// </summary>
    private static async Task<IResult> UpdatePreferences(
        UpdateOpportunityPreferencesRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var menteeId = GetUserIdFromClaims(httpContext);
        if (menteeId is null)
            return Results.Unauthorized();

        var typePreferences = new List<OpportunityType>();
        foreach (var typeStr in request.TypePreferences ?? [])
        {
            if (Enum.TryParse<OpportunityType>(typeStr, ignoreCase: true, out var parsed))
            {
                typePreferences.Add(parsed);
            }
        }

        var command = new UpdateOpportunityPreferencesCommand(
            MenteeId: menteeId.Value,
            IsEnabled: request.IsEnabled,
            TypePreferences: typePreferences,
            SkillMatchEnabled: request.SkillMatchEnabled);

        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(new { message = "Opportunity notification preferences updated." })
            : Results.BadRequest(new { error = result.Error });
    }

    private static Guid? GetUserIdFromClaims(HttpContext httpContext)
    {
        var subClaim = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (subClaim is null || !Guid.TryParse(subClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}

/// <summary>
/// Request body for updating opportunity notification preferences.
/// </summary>
public sealed record UpdateOpportunityPreferencesRequest(
    bool IsEnabled,
    List<string>? TypePreferences,
    bool SkillMatchEnabled);
