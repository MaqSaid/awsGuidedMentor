using GuidedMentor.Mentoring.Api.Endpoints;
using GuidedMentor.Mentoring.Application;
using GuidedMentor.SharedInfrastructure.FeatureFlags;
using GuidedMentor.SharedInfrastructure.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI document generation (AOT-compatible in .NET 10)
builder.Services.AddOpenApi();

// Register MediatR handlers from the Application layer assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(MentoringApplicationMarker.Assembly));

// Register AWS AppConfig feature flags
builder.Services.AddFeatureFlags(builder.Configuration);

// Register health checks for Mentoring service dependencies
builder.Services.AddHealthChecks();

var app = builder.Build();

// OpenAPI spec endpoint (anonymous — no auth required, alongside /v1/health)
app.MapOpenApi();

// Scalar API reference UI (available at /scalar/v1)
app.MapScalarApiReference();

// Health check endpoint using ASP.NET Core infrastructure
app.MapHealthCheckEndpoint();

// Map browse mentor endpoints (GET /v1/mentors, GET /v1/mentors/{id})
app.MapBrowseEndpoints();

// Map locking mechanism endpoints (POST/DELETE /v1/locks)
app.MapLockingEndpoints();

// Map session management endpoints (GET /v1/sessions, POST .../accept, .../decline, .../complete)
app.MapSessionEndpoints();

// Map mentor availability endpoints
app.MapMentorAvailabilityEndpoints();

// Map mentor settings endpoints
app.MapMentorSettingsEndpoints();

// Map opportunity board endpoints
app.MapOpportunityEndpoints();
app.MapOpportunityPreferencesEndpoints();

app.Run();
