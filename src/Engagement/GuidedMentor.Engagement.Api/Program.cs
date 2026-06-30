using System.Security.Claims;
using System.Text.Json.Serialization;
using GuidedMentor.Engagement.Application;
using GuidedMentor.Engagement.Application.Analytics;
using GuidedMentor.Engagement.Application.Analytics.DTOs;
using GuidedMentor.Engagement.Application.Commands;
using GuidedMentor.Engagement.Application.Commands.Meetups;
using GuidedMentor.Engagement.Application.Commands.Notifications;
using GuidedMentor.Engagement.Application.DTOs;
using GuidedMentor.Engagement.Application.Queries.Dashboard;
using GuidedMentor.Engagement.Application.Queries.Meetups;
using GuidedMentor.Engagement.Application.Queries.Notifications;
using GuidedMentor.Engagement.Application.Services;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.Engagement.Infrastructure;
using GuidedMentor.Engagement.Infrastructure.Repositories;
using GuidedMentor.SharedKernel;
using GuidedMentor.SharedInfrastructure.FeatureFlags;
using GuidedMentor.SharedInfrastructure.HealthChecks;
using MediatR;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON source generation for Native AOT compatibility
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, EngagementJsonContext.Default);
});

// Register OpenAPI document generation (AOT-compatible in .NET 10)
builder.Services.AddOpenApi();

// Register MediatR handlers from the Application assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateNotificationCommand).Assembly));

// Register Infrastructure services (DynamoDB, AppSync)
builder.Services.AddEngagementInfrastructure(builder.Configuration);

// Register AI Help Assistant services (plugin, rate limiter, guardrails config)
builder.Services.AddEngagementHelpAssistant(builder.Configuration);

// Register AWS AppConfig feature flags
builder.Services.AddFeatureFlags(builder.Configuration);

// Register Analytics handlers
builder.Services.AddSingleton<IEngagementEventRepository, DynamoDbEngagementEventRepository>();
builder.Services.AddSingleton<IConsentRepository, DynamoDbConsentRepository>();
builder.Services.AddSingleton<IngestEventsHandler>();
builder.Services.AddSingleton<UpdateConsentHandler>();

// Register health checks for Engagement service dependencies (DynamoDB + Bedrock + Aurora)
builder.Services.AddHealthChecks()
    .AddDynamoDbCheck("Notifications", name: "dynamodb-notifications")
    .AddDynamoDbCheck("EngagementEvents", name: "dynamodb-engagement-events")
    .AddBedrockCheck()
    .AddAuroraCheck();

var app = builder.Build();

// OpenAPI spec endpoint (anonymous — no auth required, alongside /v1/health)
app.MapOpenApi();

// Scalar API reference UI (available at /scalar/v1)
app.MapScalarApiReference();

// Health check endpoint using ASP.NET Core infrastructure
app.MapHealthCheckEndpoint();

// Notification endpoints
app.MapGet("/v1/notifications", async (Guid userId, IMediator mediator, CancellationToken ct) =>
{
    var query = new GetNotificationsQuery(userId);
    var notifications = await mediator.Send(query, ct);
    return Results.Ok(notifications);
})
.WithName("GetNotifications")
.WithTags("Notifications")
.WithDescription("Retrieves all notifications for the specified user.");


app.MapGet("/v1/notifications/count", async (Guid userId, IMediator mediator, CancellationToken ct) =>
{
    var query = new GetUnreadCountQuery(userId);
    var count = await mediator.Send(query, ct);
    return Results.Ok(new UnreadCountResponse(count));
})
.WithName("GetUnreadNotificationCount")
.WithTags("Notifications")
.WithDescription("Returns the count of unread notifications for the user.");

app.MapPut("/v1/notifications/{id:guid}/read", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var command = new MarkNotificationReadCommand(id);
    var result = await mediator.Send(command, ct);
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new ErrorResponse(result.Error));
})
.WithName("MarkNotificationRead")
.WithTags("Notifications")
.WithDescription("Marks a single notification as read.")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status400BadRequest);

app.MapPut("/v1/notifications/read-all", async (Guid userId, IMediator mediator, CancellationToken ct) =>
{
    var command = new BatchMarkReadCommand(userId);
    var result = await mediator.Send(command, ct);
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new ErrorResponse(result.Error));
})
.WithName("MarkAllNotificationsRead")
.WithTags("Notifications")
.WithDescription("Marks all notifications as read for the user.")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status400BadRequest);

// AI Help Assistant — POST /v1/assistant/chat (streaming via Server-Sent Events)
app.MapPost("/v1/assistant/chat", async (
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    // Extract user ID from JWT claims (Cognito sub)
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    // Read the request body
    var request = await httpContext.Request.ReadFromJsonAsync<ChatRequest>(ct);
    if (request is null)
    {
        return Results.BadRequest(new ErrorResponse("Invalid request body."));
    }

    // Convert request history to ChatMessage objects
    var history = (request.History ?? [])
        .Select(h => new ChatMessage(
            h.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                ? ChatRole.Assistant
                : ChatRole.User,
            h.Content))
        .ToList();

    var command = new ChatWithAssistantCommand(userId, request.Message ?? string.Empty, history);
    var result = await mediator.Send(command, ct);

    if (!result.IsSuccess)
    {
        if (result.Error.Contains("Rate limit", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Response.StatusCode = 429;
            httpContext.Response.ContentType = "application/json";
            var chatError = new ChatErrorResponse(result.Error, result.RemainingMessages);
            await httpContext.Response.WriteAsJsonAsync(
                chatError,
                EngagementJsonContext.Default.GetTypeInfo(typeof(ChatErrorResponse))!,
                cancellationToken: ct);
            return Results.Empty;
        }

        return Results.BadRequest(new ErrorResponse(result.Error));
    }

    // Stream response as Server-Sent Events for the frontend useChat() hook
    httpContext.Response.ContentType = "text/event-stream";
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";
    httpContext.Response.Headers["X-Remaining-Messages"] = result.RemainingMessages.ToString();

    await foreach (var chunk in result.ResponseStream!.WithCancellation(ct))
    {
        await httpContext.Response.WriteAsync($"data: {chunk}\n\n", ct);
        await httpContext.Response.Body.FlushAsync(ct);
    }

    // Signal end of stream
    await httpContext.Response.WriteAsync("data: [DONE]\n\n", ct);
    await httpContext.Response.Body.FlushAsync(ct);

    return Results.Empty;
});

// === Dashboard Endpoints ===

// GET /v1/dashboard/mentee — Mentee dashboard data (Requirement 10.1-10.5)
app.MapGet("/v1/dashboard/mentee", async (
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var query = new GetMenteeDashboardQuery(userId);
    var dashboard = await mediator.Send(query, ct);
    return Results.Ok(dashboard);
});

// GET /v1/dashboard/mentor — Mentor dashboard data (Requirement 11.1-11.7)
app.MapGet("/v1/dashboard/mentor", async (
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var query = new GetMentorDashboardQuery(userId);
    var dashboard = await mediator.Send(query, ct);
    return Results.Ok(dashboard);
});

// === Meetup Endpoints ===

// GET /v1/meetups — Upcoming meetups for user's chapter (Requirement 29.9)
app.MapGet("/v1/meetups", async (
    HttpContext httpContext,
    IMediator mediator,
    string? chapter,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    if (!Enum.TryParse<AustralianChapter>(chapter, ignoreCase: true, out var parsedChapter))
        return Results.BadRequest(new ErrorResponse("Invalid or missing chapter."));

    var query = new GetUpcomingMeetupsQuery(parsedChapter);
    var meetups = await mediator.Send(query, ct);
    return meetups.IsSuccess ? Results.Ok(meetups.Value) : Results.BadRequest(new ErrorResponse(meetups.Error));
});

// POST /v1/meetups — Create meetup event (chapter lead only, Requirement 29.1)
app.MapPost("/v1/meetups", async (
    CreateMeetupRequest request,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    if (!Enum.TryParse<AustralianChapter>(request.Chapter, ignoreCase: true, out var chapter))
        return Results.BadRequest(new ErrorResponse("Invalid chapter."));

    if (!TimeOnly.TryParse(request.StartTime, out var startTime))
        return Results.BadRequest(new ErrorResponse("Invalid start time format."));

    if (!TimeOnly.TryParse(request.EndTime, out var endTime))
        return Results.BadRequest(new ErrorResponse("Invalid end time format."));

    var eventDateTime = request.EventDate.ToDateTime(TimeOnly.MinValue);

    var command = new CreateMeetupEventCommand(userId, chapter, request.Title,
        eventDateTime, startTime, endTime, request.VenueName,
        request.VenueAddress, request.EventUrl ?? string.Empty);
    var result = await mediator.Send(command, ct);
    return result.IsSuccess
        ? Results.Created($"/v1/meetups/{result.Value}", new { meetupId = result.Value })
        : Results.BadRequest(new ErrorResponse(result.Error));
});

// POST /v1/meetups/{meetupId}/cancel — Cancel meetup (chapter lead only, Requirement 29.6)
app.MapPost("/v1/meetups/{meetupId:guid}/cancel", async (
    Guid meetupId,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var command = new CancelMeetupEventCommand(userId, meetupId);
    var result = await mediator.Send(command, ct);
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new ErrorResponse(result.Error));
});

// POST /v1/meetups/{meetupId}/attend — Confirm attendance (Requirement 29.4)
app.MapPost("/v1/meetups/{meetupId:guid}/attend", async (
    Guid meetupId,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var command = new ConfirmMeetupAttendanceCommand(userId, meetupId);
    var result = await mediator.Send(command, ct);
    return result.IsSuccess ? Results.Ok(new { status = "attending" }) : Results.BadRequest(new ErrorResponse(result.Error));
});

// POST /v1/sessions/{sessionId}/align-meetup — Align session to meetup (Requirement 29.3)
app.MapPost("/v1/sessions/{sessionId:guid}/align-meetup", async (
    Guid sessionId,
    AlignMeetupRequest request,
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var command = new AlignSessionToMeetupCommand(sessionId, request.MeetupId);
    var result = await mediator.Send(command, ct);
    return result.IsSuccess ? Results.Ok(new { status = "aligned" }) : Results.BadRequest(new ErrorResponse(result.Error));
});

// POST /v1/analytics/events — Batch ingest tracked events (Requirement 30.2, 30.3)
app.MapPost("/v1/analytics/events", async (
    IngestEventsRequest request,
    IngestEventsHandler handler,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
    {
        // For sendBeacon requests that may not carry auth, use anonymous tracking
        userId = Guid.Empty;
    }

    var command = new IngestEventsCommand(userId, request.Events);
    var result = await handler.HandleAsync(command, ct);

    return result.IsSuccess
        ? Results.Ok(new IngestEventsResponse(request.Events.Count))
        : Results.BadRequest(new ErrorResponse(result.Error));
});

// PUT /v1/analytics/consent — User opts in/out of tracking (Requirement 30.7, 30.8)
app.MapPut("/v1/analytics/consent", async (
    UpdateConsentRequest request,
    UpdateConsentHandler handler,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? httpContext.User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var command = new UpdateConsentCommand(userId, request.Consent);
    var result = await handler.HandleAsync(command, ct);

    return result.IsSuccess
        ? Results.Ok(new ConsentResponse(request.Consent))
        : Results.BadRequest(new ErrorResponse(result.Error));
});

// GET /v1/analytics/dashboard — Operator analytics dashboard (admin only, Requirement 30.4, 30.6)
app.MapGet("/v1/analytics/dashboard", async (
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    if (!IsAdmin(httpContext))
        return Results.Forbid();

    var query = new GetAnalyticsDashboardQuery();
    var dashboard = await mediator.Send(query, ct);
    return Results.Ok(dashboard);
});

// GET /v1/analytics/funnels — Conversion funnel analysis (admin only, Requirement 30.6)
app.MapGet("/v1/analytics/funnels", async (
    HttpContext httpContext,
    IMediator mediator,
    CancellationToken ct) =>
{
    if (!IsAdmin(httpContext))
        return Results.Forbid();

    var query = new GetFunnelDataQuery();
    var funnelData = await mediator.Send(query, ct);
    return Results.Ok(funnelData);
});

// GET /v1/analytics/engagement — Engagement-specific analytics (admin only, Requirement 30.5)
app.MapGet("/v1/analytics/engagement", async (
    HttpContext httpContext,
    IMediator mediator,
    DateOnly? from,
    DateOnly? to,
    CancellationToken ct) =>
{
    if (!IsAdmin(httpContext))
        return Results.Forbid();

    var query = new GetEngagementAnalyticsQuery(from, to);
    var analytics = await mediator.Send(query, ct);
    return Results.Ok(analytics);
});

app.Run();

// Admin check helper — verifies Super_Admin role in JWT claims
static bool IsAdmin(HttpContext httpContext)
{
    var roleClaims = httpContext.User.FindAll(ClaimTypes.Role)
        .Concat(httpContext.User.FindAll("cognito:groups"));
    return roleClaims.Any(c =>
        c.Value.Equals("Super_Admin", StringComparison.OrdinalIgnoreCase) ||
        c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase));
}

// AOT-compatible response types
internal sealed record UnreadCountResponse(int UnreadCount);
internal sealed record ErrorResponse(string Error);
internal sealed record ChatErrorResponse(string Error, int RemainingMessages);
internal sealed record ChatRequest(string? Message, List<ChatHistoryItem>? History);
internal sealed record ChatHistoryItem(string Role, string Content);
internal sealed record IngestEventsRequest(IReadOnlyList<IngestEventDto> Events);
internal sealed record UpdateConsentRequest(string Consent);
internal sealed record IngestEventsResponse(int Ingested);
internal sealed record ConsentResponse(string Status);
internal sealed record CreateMeetupRequest(string Chapter, string Title, DateOnly EventDate, string StartTime, string EndTime, string VenueName, string VenueAddress, string? EventUrl);
internal sealed record AlignMeetupRequest(Guid MeetupId);

// Source-generated JSON serializer context for Native AOT
[JsonSerializable(typeof(NotificationDto))]
[JsonSerializable(typeof(IReadOnlyList<NotificationDto>))]
[JsonSerializable(typeof(List<NotificationDto>))]
[JsonSerializable(typeof(UnreadCountResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ChatErrorResponse))]
[JsonSerializable(typeof(ChatRequest))]
[JsonSerializable(typeof(ChatHistoryItem))]
[JsonSerializable(typeof(IngestEventsRequest))]
[JsonSerializable(typeof(IngestEventDto))]
[JsonSerializable(typeof(UpdateConsentRequest))]
[JsonSerializable(typeof(IngestEventsResponse))]
[JsonSerializable(typeof(ConsentResponse))]
[JsonSerializable(typeof(CreateMeetupRequest))]
[JsonSerializable(typeof(AlignMeetupRequest))]
[JsonSerializable(typeof(MenteeDashboardDto))]
[JsonSerializable(typeof(MentorDashboardDto))]
[JsonSerializable(typeof(MeetupEventDto))]
[JsonSerializable(typeof(IReadOnlyList<MeetupEventDto>))]
[JsonSerializable(typeof(AnalyticsDashboardDto))]
[JsonSerializable(typeof(ActiveUsersMetrics))]
[JsonSerializable(typeof(FeatureUsageDto))]
[JsonSerializable(typeof(ErrorHotspotDto))]
[JsonSerializable(typeof(RetentionMetrics))]
[JsonSerializable(typeof(FunnelDataDto))]
[JsonSerializable(typeof(FunnelStageDto))]
[JsonSerializable(typeof(EngagementAnalyticsDto))]
[JsonSerializable(typeof(EngagementMetricBreakdownDto))]
internal partial class EngagementJsonContext : JsonSerializerContext
{
}
