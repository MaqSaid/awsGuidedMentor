using System.Security.Claims;
using System.Text.Json;
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
using GuidedMentor.SharedInfrastructure.Data.Entities;
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

// Disable strict DI validation in dev (some handlers have unregistered optional dependencies)
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});

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

// Onboarding repositories (in-memory for local dev)
builder.Services.AddSingleton<GuidedMentor.Identity.Application.Interfaces.IOnboardingProgressRepository,
    MockOnboardingProgressRepository>();
builder.Services.AddSingleton<GuidedMentor.Identity.Application.Interfaces.IMenteeProfileRepository,
    MockMenteeProfileRepository>();
builder.Services.AddSingleton<GuidedMentor.Identity.Application.Interfaces.IMentorProfileRepository,
    MockMentorProfileRepository>();

// Mentoring repositories
builder.Services.AddScoped<IMentorRepository, PostgresMentorRepository>();
builder.Services.AddScoped<IMenteeRepository, PostgresMenteeRepository>();
builder.Services.AddScoped<ISessionRepository, PostgresSessionRepository>();
builder.Services.AddScoped<IOpportunityRepository, PostgresOpportunityRepository>();
builder.Services.AddSingleton<IMentoringNotificationPublisher, MockNotificationPublisher>();

// Content repositories
builder.Services.AddScoped<ISessionPlanRepository, PostgresSessionPlanRepository>();

// Engagement repositories (PostgreSQL-backed no-op implementations)
builder.Services.AddScoped<GuidedMentor.Engagement.Domain.Repositories.INotificationRepository,
    GuidedMentor.Engagement.Infrastructure.Persistence.PostgresNotificationRepository>();
builder.Services.AddScoped<GuidedMentor.Engagement.Domain.Repositories.IConsentRepository,
    GuidedMentor.Engagement.Infrastructure.Repositories.PostgresConsentRepository>();
builder.Services.AddScoped<GuidedMentor.Engagement.Domain.Repositories.IEngagementEventRepository,
    GuidedMentor.Engagement.Infrastructure.Repositories.PostgresEngagementEventRepository>();
builder.Services.AddScoped<GuidedMentor.Engagement.Application.Interfaces.IAppSyncNotificationPublisher,
    GuidedMentor.Engagement.Infrastructure.RealTime.NoOpNotificationPublisher>();

// Analytics repository (mock — returns sample data without Aurora)
builder.Services.AddSingleton<GuidedMentor.Engagement.Application.Interfaces.IAnalyticsRepository,
    MockAnalyticsRepository>();

// EventBridge publisher (mock — logs events to console)
builder.Services.AddSingleton<GuidedMentor.Mentoring.Application.Interfaces.IEventBridgePublisher,
    MockEventBridgePublisher>();

// SignalR for real-time notifications
builder.Services.AddSignalR();

// Hangfire background jobs
var hangfireConnectionString = "Server=localhost;Port=5432;Database=guidedmentor;User Id=dev;Password=dev";
builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(options =>
    options.UseNpgsqlConnection(hangfireConnectionString)));
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
    cfg.RegisterServicesFromAssembly(typeof(GuidedMentor.Mentoring.Api.Handlers.BrowseMentorsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GuidedMentor.Content.Application.Commands.GenerateSessionPlanCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GuidedMentor.Engagement.Application.Commands.Notifications.CreateNotificationCommand).Assembly);
});

// AI Help Assistant services (real except for IChatClient which is mocked above)
builder.Services.AddEngagementHelpAssistant(builder.Configuration);

// Health checks
builder.Services.AddHealthChecks();

// CORS — configurable for production (Cloudflare Pages URL) and local dev
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:3001",
                    "http://localhost:3002",
                    "http://localhost:3003",
                    "http://localhost:3004")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            var allowedOrigins = builder.Configuration["CORS:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                ?? ["https://guidedmentor.pages.dev"];
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Authentication — real JWT in production, mock in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication("DevScheme")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevAuthHandler>("DevScheme", null);
}
else
{
    var jwtSecret = builder.Configuration["Jwt:Secret"]!;
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtSecret)),
            };
        });
}
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
app.MapGet("/v1/health", (IWebHostEnvironment env) => Results.Ok(new { status = "healthy", environment = env.EnvironmentName }));

// SignalR notification hub
app.MapHub<NotificationHub>("/hubs/notifications");

// Hangfire recurring jobs (use service-based API, not static)
var jobManager = app.Services.GetRequiredService<IRecurringJobManager>();
jobManager.AddOrUpdate<CleanupExpiredTokensJob>("cleanup-tokens", j => j.ExecuteAsync(), "*/5 * * * *");
jobManager.AddOrUpdate<OpportunityExpiryJob>("expire-opportunities", j => j.ExecuteAsync(), "0 0 * * *");
jobManager.AddOrUpdate<LockExpiryJob>("expire-locks", j => j.ExecuteAsync(), "*/5 * * * *");
jobManager.AddOrUpdate<SessionEscalationJob>("escalate-sessions", j => j.ExecuteAsync(), "0 0 * * *");
jobManager.AddOrUpdate<CompletionReminderJob>("completion-reminders", j => j.ExecuteAsync(), "0 0 * * *");

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

// Meetups endpoint (inline — queries PostgreSQL meetups table)
app.MapGet("/v1/meetups", async (GuidedMentorDbContext db, string? chapter, CancellationToken ct) =>
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var query = db.Meetups
        .Where(m => !m.IsCancelled && m.EventDate >= today);

    if (!string.IsNullOrWhiteSpace(chapter))
        query = query.Where(m => m.Chapter == chapter);

    var meetups = await query
        .OrderBy(m => m.EventDate)
        .ThenBy(m => m.StartTime)
        .Select(m => new
        {
            m.Id,
            m.Chapter,
            m.Title,
            m.EventDate,
            m.StartTime,
            m.EndTime,
            m.VenueName,
            m.VenueAddress,
            m.EventUrl,
            m.CreatedBy,
            AttendeeCount = m.ConfirmedAttendees.Length
        })
        .ToListAsync(ct);

    return Results.Ok(meetups);
})
.WithName("GetMeetupsLocal")
.WithTags("Meetups");

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

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("=== GuidedMentor Local Dev Server ===");
    Console.WriteLine("API:        http://localhost:5000");
    Console.WriteLine("Docs:       http://localhost:5000/scalar/v1");
    Console.WriteLine("OpenAPI:    http://localhost:5000/openapi/v1.json");
    Console.WriteLine("PostgreSQL: localhost:5432");
    Console.WriteLine("=====================================");
    app.Run("http://localhost:5000");
}
else
{
    // Production — Render sets PORT env variable
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    Console.WriteLine($"=== GuidedMentor Production Server (port {port}) ===");
    app.Run($"http://0.0.0.0:{port}");
}

// ========== Supporting Records ==========
sealed record ChatRequest(string? Message, List<ChatHistoryItem>? History);
sealed record ChatHistoryItem(string Role, string Content);
