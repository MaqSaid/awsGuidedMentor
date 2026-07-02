using System.Security.Claims;
using System.Text.Json.Serialization;
using GuidedMentor.Content.Application.Commands;
using GuidedMentor.SharedInfrastructure.FeatureFlags;
using GuidedMentor.SharedInfrastructure.HealthChecks;
using MediatR;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON source generation for Native AOT compatibility
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ContentJsonContext.Default);
});

// Register OpenAPI document generation (AOT-compatible in .NET 10)
builder.Services.AddOpenApi();

// Register MediatR handlers from the Application assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GenerateSessionPlanCommand).Assembly));

// Register AWS AppConfig feature flags
builder.Services.AddFeatureFlags(builder.Configuration);

// Register health checks for Content service dependencies
builder.Services.AddHealthChecks();

var app = builder.Build();

// OpenAPI spec endpoint (anonymous — no auth required, alongside /v1/health)
app.MapOpenApi();

// Scalar API reference UI (available at /scalar/v1)
app.MapScalarApiReference();

// Health check endpoint using ASP.NET Core infrastructure
app.MapHealthCheckEndpoint();

// GET /v1/sessions/{sessionId}/plan — Get the generated session plan
app.MapGet("/v1/sessions/{sessionId:guid}/plan", async (
    Guid sessionId,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userId = GetUserId(httpContext);
    if (userId is null) return Results.Unauthorized();

    var query = new GetSessionPlanQuery(userId.Value, sessionId);
    var result = await mediator.Send(query, ct);
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetSessionPlan")
.WithTags("SessionPlans")
.WithDescription("Retrieves the AI-generated session plan for a given session.")
.Produces<SessionPlanDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status401Unauthorized)
.RequireAuthorization();

// POST /v1/sessions/{sessionId}/plan/generate — Trigger session plan generation
app.MapPost("/v1/sessions/{sessionId:guid}/plan/generate", async (
    Guid sessionId,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userId = GetUserId(httpContext);
    if (userId is null) return Results.Unauthorized();

    var command = new RequestSessionPlanGenerationCommand(sessionId, userId.Value);
    var result = await mediator.Send(command, ct);

    return result.IsSuccess
        ? Results.Accepted($"/v1/sessions/{sessionId}/plan", new { Status = "generating" })
        : Results.BadRequest(new ContentErrorResponse(result.Error));
})
.WithName("GenerateSessionPlan")
.WithTags("SessionPlans")
.WithDescription("Triggers AI generation of a session plan for the specified session.")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.RequireAuthorization();

// PUT /v1/sessions/{sessionId}/plan/checklist — Update checklist items (toggle complete)
app.MapPut("/v1/sessions/{sessionId:guid}/plan/checklist", async (
    Guid sessionId,
    UpdateChecklistRequest request,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userId = GetUserId(httpContext);
    if (userId is null) return Results.Unauthorized();

    var command = new UpdateChecklistCommand(userId.Value, sessionId, request.ItemId, request.IsCompleted);
    var result = await mediator.Send(command, ct);

    return result.IsSuccess
        ? Results.Ok(new { Progress = result.Value })
        : Results.BadRequest(new ContentErrorResponse(result.Error));
})
.WithName("UpdateChecklist")
.WithTags("SessionPlans")
.WithDescription("Toggles completion status of a checklist item in the session plan.")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.RequireAuthorization();

// GET /v1/sessions/{sessionId}/plan/stream — Server-Sent Events for real-time plan generation
app.MapGet("/v1/sessions/{sessionId:guid}/plan/stream", async (
    Guid sessionId,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userId = GetUserId(httpContext);
    if (userId is null)
    {
        httpContext.Response.StatusCode = 401;
        return;
    }

    httpContext.Response.ContentType = "text/event-stream";
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";

    var query = new StreamSessionPlanQuery(userId.Value, sessionId);
    var stream = await mediator.Send(query, ct);

    if (stream is null)
    {
        await httpContext.Response.WriteAsync("data: {\"error\":\"Plan not found\"}\n\n", ct);
        return;
    }

    await foreach (var chunk in stream.WithCancellation(ct))
    {
        await httpContext.Response.WriteAsync($"data: {chunk}\n\n", ct);
        await httpContext.Response.Body.FlushAsync(ct);
    }

    await httpContext.Response.WriteAsync("data: [DONE]\n\n", ct);
    await httpContext.Response.Body.FlushAsync(ct);
})
.WithName("StreamSessionPlan")
.WithTags("SessionPlans")
.WithDescription("Streams AI-generated session plan content via Server-Sent Events.")
.Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
.Produces(StatusCodes.Status401Unauthorized)
.RequireAuthorization();

app.Run();

// Helper
static Guid? GetUserId(HttpContext httpContext)
{
    var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("sub")?.Value;
    return Guid.TryParse(claim, out var userId) ? userId : null;
}

// Request/Response DTOs
internal sealed record UpdateChecklistRequest(string ItemId, bool IsCompleted);
internal sealed record ContentErrorResponse(string Error);

// Query records for Content context (handlers exist in Application layer)
public sealed record GetSessionPlanQuery(Guid UserId, Guid SessionId) : IRequest<SessionPlanDto?>;
public sealed record StreamSessionPlanQuery(Guid UserId, Guid SessionId) : IRequest<IAsyncEnumerable<string>?>;
public sealed record UpdateChecklistCommand(Guid UserId, Guid SessionId, string ItemId, bool IsCompleted) : IRequest<GuidedMentor.SharedKernel.Result<int>>;

public sealed record SessionPlanDto(
    Guid SessionId,
    string SessionTitle,
    IReadOnlyList<AgendaItemDto> Agenda,
    IReadOnlyList<ChecklistItemDto> PreworkTasks,
    IReadOnlyList<ChecklistItemDto> FollowUpTasks,
    int ProgressPercent,
    string Status);

public sealed record AgendaItemDto(string Title, int DurationMinutes, string Description);
public sealed record ChecklistItemDto(string Id, string Title, bool IsCompleted);

// Source-generated JSON serializer context for Native AOT
[JsonSerializable(typeof(SessionPlanDto))]
[JsonSerializable(typeof(AgendaItemDto))]
[JsonSerializable(typeof(ChecklistItemDto))]
[JsonSerializable(typeof(IReadOnlyList<AgendaItemDto>))]
[JsonSerializable(typeof(IReadOnlyList<ChecklistItemDto>))]
[JsonSerializable(typeof(UpdateChecklistRequest))]
[JsonSerializable(typeof(ContentErrorResponse))]
internal partial class ContentJsonContext : JsonSerializerContext
{
}
