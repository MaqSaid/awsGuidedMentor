using GuidedMentor.Mentoring.Application.Commands.Settings;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Api.Endpoints;

/// <summary>
/// Maps /v1/mentors/me/settings endpoint for mentor profile settings updates.
/// </summary>
public static class MentorSettingsEndpoints
{
    public static RouteGroupBuilder MapMentorSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/mentors/me/settings")
            .WithTags("Mentor Settings");

        group.MapPut("/", UpdateSettings)
            .WithName("UpdateMentorSettings")
            .WithSummary("Update mentor profile settings with onboarding-level validation");

        return group;
    }

    /// <summary>
    /// PUT /v1/mentors/me/settings — Updates the mentor's profile settings.
    /// Validates inputs using the same rules as onboarding and enforces maxMentees constraint.
    /// </summary>
    private static async Task<IResult> UpdateSettings(
        UpdateMentorSettingsRequest request,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken ct)
    {
        var mentorId = GetMentorIdFromClaims(httpContext);
        if (mentorId is null)
            return Results.Unauthorized();

        if (!Enum.TryParse<AustralianChapter>(request.Chapter, ignoreCase: true, out var chapter))
        {
            return Results.BadRequest(new { error = "Invalid chapter. Must be a valid Australian chapter." });
        }

        var command = new UpdateSettingsCommand(
            MentorId: mentorId.Value,
            DisplayName: request.DisplayName,
            ProfessionalTitle: request.ProfessionalTitle,
            CompanyName: request.CompanyName,
            Chapter: chapter,
            ExpertiseAreas: request.ExpertiseAreas,
            YearsOfExperience: request.YearsOfExperience,
            Certifications: request.Certifications,
            Topics: request.Topics,
            MaxMentees: request.MaxMentees,
            SessionFormats: request.SessionFormats,
            Bio: request.Bio);

        var result = await mediator.Send(command, ct);

        return result.IsSuccess
            ? Results.Ok(new { message = "Mentor settings updated successfully." })
            : Results.BadRequest(new { error = result.Error });
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
/// Request body for updating mentor settings.
/// All fields follow the same validation rules as mentor onboarding.
/// </summary>
public sealed record UpdateMentorSettingsRequest(
    string DisplayName,
    string ProfessionalTitle,
    string CompanyName,
    string Chapter,
    IReadOnlyList<string> ExpertiseAreas,
    int YearsOfExperience,
    IReadOnlyList<string> Certifications,
    IReadOnlyList<string> Topics,
    int MaxMentees,
    IReadOnlyList<string> SessionFormats,
    string Bio);
