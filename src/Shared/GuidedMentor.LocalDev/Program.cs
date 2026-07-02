using System.Text.Json.Serialization;
using GuidedMentor.Content.Application.Interfaces;
using GuidedMentor.Content.Infrastructure.Repositories;
using GuidedMentor.Identity.Api.Endpoints;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Infrastructure.Auth;
using GuidedMentor.Identity.Infrastructure.Repositories;
using GuidedMentor.Mentoring.Api.Endpoints;
using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.Mentoring.Infrastructure.Repositories;
using GuidedMentor.Engagement.Application;
using GuidedMentor.Engagement.Application.Services;
using GuidedMentor.LocalDev.Mocks;
using GuidedMentor.SharedInfrastructure.Data;
using GuidedMentor.SharedInfrastructure.Email;
using GuidedMentor.SharedInfrastructure.FeatureFlags;
using GuidedMentor.SharedInfrastructure.HealthChecks;
using GuidedMentor.SharedInfrastructure.Hubs;
using GuidedMentor.SharedInfrastructure.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ========== Configure Services ==========

// JSON serialization (relaxed in local dev — no AOT source generation needed)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// OpenAPI
builder.Services.AddOpenApi();

// PostgreSQL via EF Core (replaces DynamoDB Local)
builder.Services.AddDbContext<GuidedMentorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Gmail SMTP email sender (replaces SES)
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<IEmailSender, GmailSmtpEmailSender>();

// JWT token generation (self-hosted, replaces Cognito)
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();

// Identity repositories
builder.Services.AddScoped<PostgresUserRepository>();
builder.Services.AddScoped<GuidedMentor.Identity.Domain.Repositories.IUserRepository>(sp => sp.GetRequiredService<PostgresUserRepository>());
builder.Services.AddScoped<GuidedMentor.Identity.Application.Interfaces.IUserRepository>(sp => sp.GetRequiredService<PostgresUserRepository>());
builder.Services.AddScoped<IMagicLinkService, PostgresMagicLinkService>();

// Mentoring repositories
builder.Services.AddScoped<IMentorRepository, PostgresMentorRepository>();
builder.Services.AddScoped<IMenteeRepository, PostgresMenteeRepository>();
builder.Services.AddScoped<ISessionRepository, PostgresSessionRepository>();
builder.Services.AddScoped<IOpportunityRepository, PostgresOpportunityRepository>();

// Content repositories
builder.Services.AddScoped<ISessionPlanRepository, PostgresSessionPlanRepository>();

// SignalR for real-time notifications
builder.Services.AddSignalR();

// Hangfire background jobs
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

// Mock AI client (canned responses — zero tokens consumed)
builder.Services.AddSingleton<IChatClient, MockChatClient>();

// Mock intent classifier (always returns PlatformHelp in local dev — skips HF API)
builder.Services.AddSingleton<IIntentClassifier, MockIntentClassifier>();

// FAQ lookup (real — uses embedded JSON)
builder.Services.AddSingleton<FaqLookupService>();

// Feature flags (all enabled in local dev)
builder.Services.AddFeatureFlags(builder.Configuration);

// MediatR — register all bounded context handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GuidedMentor.Identity.Application.IdentityApplicationMarker).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GuidedMentor.Mentoring.Application.MentoringApplicationMarker).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GuidedMentor.Content.Application.Commands.GenerateSessionPlanCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GuidedMentor.Engagement.Application.Commands.Notifications.CreateNotificationCommand).Assembly);
});

// AI Help Assistant services (real except for IChatClient which is mocked above)
builder.Services.AddEngagementHelpAssistant(builder.Configuration);

// Health checks
builder.Services.AddHealthChecks();

// CORS for frontend (localhost:3000-3004)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:3002",
                "http://localhost:3003",
                "http://localhost:3004")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Mock authentication (accepts any Bearer token as valid)
builder.Services.AddAuthentication("DevScheme")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevAuthHandler>("DevScheme", null);
builder.Services.AddAuthorization();

var app = builder.Build();

// ========== Middleware Pipeline ==========

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// OpenAPI + Scalar docs
app.MapOpenApi();
app.MapScalarApiReference();

// Health check
app.MapGet("/v1/health", () => Results.Ok(new { status = "healthy", environment = "local-dev" }));

// SignalR notification hub
app.MapHub<NotificationHub>("/hubs/notifications");

// Hangfire recurring jobs
RecurringJob.AddOrUpdate<CleanupExpiredTokensJob>("cleanup-tokens", j => j.ExecuteAsync(), "*/5 * * * *");
RecurringJob.AddOrUpdate<OpportunityExpiryJob>("expire-opportunities", j => j.ExecuteAsync(), "0 0 * * *");

// ========== Map ALL bounded context endpoints ==========

// Identity endpoints
app.MapAuthEndpoints();

// Mentoring endpoints
app.MapBrowseEndpoints();
app.MapLockingEndpoints();
app.MapSessionEndpoints();
app.MapMentorAvailabilityEndpoints();
app.MapMentorSettingsEndpoints();
app.MapOpportunityEndpoints();
app.MapOpportunityPreferencesEndpoints();

// Content endpoints (inline — Content uses inline MapGet in its own Program.cs)
app.MapGet("/v1/sessions/{sessionId:guid}/plan", (Guid sessionId) =>
    Results.Ok(MockSessionPlans.GetMockPlan(sessionId)))
    .WithName("GetSessionPlanLocal")
    .WithTags("SessionPlans");

app.MapPost("/v1/sessions/{sessionId:guid}/plan/generate", (Guid sessionId) =>
    Results.Accepted($"/v1/sessions/{sessionId}/plan", new { Status = "generating" }))
    .WithName("GenerateSessionPlanLocal")
    .WithTags("SessionPlans");

// Engagement endpoints (notifications, dashboard, assistant, meetups, analytics)
app.MapGet("/v1/notifications", () => Results.Ok(Array.Empty<object>()))
    .WithName("GetNotificationsLocal")
    .WithTags("Notifications");

app.MapGet("/v1/notifications/count", () => Results.Ok(new { unreadCount = 3 }))
    .WithName("GetUnreadCountLocal")
    .WithTags("Notifications");

app.MapGet("/v1/dashboard/mentee", () => Results.Ok(MockDashboards.GetMenteeDashboard()))
    .WithName("MenteeDashboardLocal")
    .WithTags("Dashboard");

app.MapGet("/v1/dashboard/mentor", () => Results.Ok(MockDashboards.GetMentorDashboard()))
    .WithName("MentorDashboardLocal")
    .WithTags("Dashboard");

// AI Help Assistant (real FAQ + mock Bedrock)
app.MapPost("/v1/assistant/chat", async (
    HttpContext httpContext,
    MediatR.IMediator mediator,
    CancellationToken ct) =>
{
    // Simplified streaming for local dev
    var request = await httpContext.Request.ReadFromJsonAsync<ChatRequest>(ct);
    if (request is null) return Results.BadRequest("Invalid request");

    var history = new List<ChatMessage>();
    var command = new GuidedMentor.Engagement.Application.Commands.ChatWithAssistantCommand(
        Guid.Parse("11111111-1111-1111-1111-111111111111"), // dev user
        request.Message ?? "",
        history);

    var result = await mediator.Send(command, ct);
    if (!result.IsSuccess) return Results.BadRequest(new { error = result.Error });

    // Stream the response via SSE
    httpContext.Response.ContentType = "text/event-stream";
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";

    await foreach (var chunk in result.ResponseStream!.WithCancellation(ct))
    {
        await httpContext.Response.WriteAsync($"data: {chunk}\n\n", ct);
        await httpContext.Response.Body.FlushAsync(ct);
    }

    await httpContext.Response.WriteAsync("data: [DONE]\n\n", ct);
    await httpContext.Response.Body.FlushAsync(ct);
    return Results.Empty;
})
.WithName("ChatAssistantLocal")
.WithTags("Assistant");

Console.WriteLine("=== GuidedMentor Local Dev Server ===");
Console.WriteLine("API:        http://localhost:5000");
Console.WriteLine("Docs:       http://localhost:5000/scalar/v1");
Console.WriteLine("OpenAPI:    http://localhost:5000/openapi/v1.json");
Console.WriteLine("PostgreSQL: localhost:5432");
Console.WriteLine("=====================================");

app.Run("http://localhost:5000");

// ========== Supporting Records ==========
sealed record ChatRequest(string? Message, List<ChatHistoryItem>? History);
sealed record ChatHistoryItem(string Role, string Content);
