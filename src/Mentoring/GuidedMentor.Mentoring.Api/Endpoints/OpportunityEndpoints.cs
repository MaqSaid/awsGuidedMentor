using GuidedMentor.Mentoring.Application.Commands.Opportunities;
using GuidedMentor.Mentoring.Application.Queries.Opportunities;
using GuidedMentor.Mentoring.Domain.Entities;
using MediatR;

namespace GuidedMentor.Mentoring.Api.Endpoints;

/// <summary>
/// Maps /v1/opportunities endpoints for the Opportunities Board feature.
/// </summary>
public static class OpportunityEndpoints
{
    public static RouteGroupBuilder MapOpportunityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/opportunities")
            .WithTags("Opportunities Board");

        group.MapPost("/", CreateOpportunity)
            .WithName("CreateOpportunity")
            .WithSummary("Create a new opportunity posting (mentor only, max 5 active)");

        group.MapGet("/", BrowseOpportunities)
            .WithName("BrowseOpportunities")
            .WithSummary("Browse all active opportunity postings with filters");

        group.MapGet("/mentor/{mentorId:guid}", GetMentorOpportunities)
            .WithName("GetMentorOpportunities")
            .WithSummary("Get all opportunity postings for a specific mentor");

        group.MapPost("/{id:guid}/renew", RenewOpportunity)
            .WithName("RenewOpportunity")
            .WithSummary("Renew an expired job posting (jobs only, extends by 30 days)");

        group.MapDelete("/{id:guid}", ArchiveOpportunity)
            .WithName("ArchiveOpportunity")
            .WithSummary("Archive an opportunity posting");

        return group;
    }

    /// <summary>
    /// POST /v1/opportunities — Creates a new opportunity posting.
    /// </summary>
    private static async Task<IResult> CreateOpportunity(
        CreateOpportunityRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var mentorId = GetUserIdFromClaims(httpContext);
        if (mentorId is null)
            return Results.Unauthorized();

        if (!Enum.TryParse<OpportunityType>(request.Type, ignoreCase: true, out var type))
        {
            return Results.BadRequest(new { error = "Invalid opportunity type. Must be 'job', 'workshop', 'event', or 'training'." });
        }

        EmploymentType? employmentType = null;
        if (request.EmploymentType is not null)
        {
            if (!Enum.TryParse<EmploymentType>(request.EmploymentType, ignoreCase: true, out var parsedEmployment))
            {
                return Results.BadRequest(new { error = "Invalid employment type." });
            }
            employmentType = parsedEmployment;
        }

        if (!Enum.TryParse<ExperienceLevel>(request.RequiredExperience, ignoreCase: true, out var experience))
        {
            return Results.BadRequest(new { error = "Invalid experience level. Must be 'beginner', 'intermediate', 'advanced', or 'any'." });
        }

        var command = new CreateOpportunityCommand(
            MentorId: mentorId.Value,
            Title: request.Title,
            Type: type,
            OrganisationName: request.OrganisationName,
            Description: request.Description,
            Location: request.Location,
            EventDateTime: request.EventDateTime,
            EmploymentType: employmentType,
            RequiredSkills: request.RequiredSkills ?? [],
            RequiredExperience: experience,
            ExternalUrl: request.ExternalUrl);

        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Created($"/v1/opportunities/{result.Value}", new { postingId = result.Value })
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// GET /v1/opportunities — Browse active postings with optional filters.
    /// </summary>
    private static async Task<IResult> BrowseOpportunities(
        string? type,
        string? location,
        string? skills,
        string? experience,
        int page = 1,
        int pageSize = 12,
        IMediator mediator = default!,
        CancellationToken ct = default)
    {
        OpportunityType? typeFilter = null;
        if (type is not null && Enum.TryParse<OpportunityType>(type, ignoreCase: true, out var parsedType))
        {
            typeFilter = parsedType;
        }

        ExperienceLevel? experienceFilter = null;
        if (experience is not null && Enum.TryParse<ExperienceLevel>(experience, ignoreCase: true, out var parsedExp))
        {
            experienceFilter = parsedExp;
        }

        IReadOnlyList<string>? skillsFilter = null;
        if (!string.IsNullOrWhiteSpace(skills))
        {
            skillsFilter = skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var query = new BrowseOpportunitiesQuery(
            TypeFilter: typeFilter,
            LocationFilter: location,
            SkillsFilter: skillsFilter,
            ExperienceFilter: experienceFilter,
            Page: page > 0 ? page : 1,
            PageSize: pageSize > 0 ? Math.Min(pageSize, 50) : 12);

        var result = await mediator.Send(query, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// GET /v1/opportunities/mentor/{mentorId} — Get a mentor's own postings.
    /// </summary>
    private static async Task<IResult> GetMentorOpportunities(
        Guid mentorId,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetMentorOpportunitiesQuery(mentorId);
        var result = await mediator.Send(query, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error });
    }

    /// <summary>
    /// POST /v1/opportunities/{id}/renew — Renew an expired job posting.
    /// </summary>
    private static async Task<IResult> RenewOpportunity(
        Guid id,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var mentorId = GetUserIdFromClaims(httpContext);
        if (mentorId is null)
            return Results.Unauthorized();

        var command = new RenewOpportunityCommand(id, mentorId.Value);
        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(new { message = "Opportunity posting renewed for another 30 days." })
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// DELETE /v1/opportunities/{id} — Archive an opportunity posting.
    /// </summary>
    private static async Task<IResult> ArchiveOpportunity(
        Guid id,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var mentorId = GetUserIdFromClaims(httpContext);
        if (mentorId is null)
            return Results.Unauthorized();

        var command = new ArchiveOpportunityCommand(id, mentorId.Value);
        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(new { message = "Opportunity posting archived." })
            : Results.BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Extracts the user ID from JWT claims.
    /// </summary>
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
/// Request body for creating an opportunity posting.
/// </summary>
public sealed record CreateOpportunityRequest(
    string Title,
    string Type,
    string OrganisationName,
    string Description,
    string Location,
    DateTime? EventDateTime,
    string? EmploymentType,
    List<string>? RequiredSkills,
    string RequiredExperience,
    string ExternalUrl);
