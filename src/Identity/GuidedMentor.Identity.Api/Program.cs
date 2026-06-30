using GuidedMentor.Identity.Api.Endpoints;
using GuidedMentor.Identity.Api.Middleware;
using GuidedMentor.Identity.Application;
using GuidedMentor.SharedInfrastructure.FeatureFlags;
using GuidedMentor.SharedInfrastructure.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI document generation (AOT-compatible in .NET 10)
builder.Services.AddOpenApi();

// Register MediatR handlers from the Application layer assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(IdentityApplicationMarker.Assembly));

// Register AWS AppConfig feature flags
builder.Services.AddFeatureFlags(builder.Configuration);

// Register health checks for Identity service dependencies
builder.Services.AddHealthChecks()
    .AddDynamoDbCheck("Users", name: "dynamodb-users")
    .AddDynamoDbCheck("Mentors", name: "dynamodb-mentors")
    .AddDynamoDbCheck("Mentees", name: "dynamodb-mentees");

var app = builder.Build();

// Maintenance mode middleware — must run before endpoint routing
// Checks if platform is in maintenance mode and blocks non-admin requests with 503
app.UseMaintenanceMode();

// OpenAPI spec endpoint (anonymous — no auth required, alongside /v1/health)
app.MapOpenApi();

// Scalar API reference UI (available at /scalar/v1)
app.MapScalarApiReference();

// Health check endpoint using ASP.NET Core infrastructure
app.MapHealthCheckEndpoint();

// Map authentication, role, onboarding, file upload, and settings endpoints
app.MapAuthEndpoints();

// Map admin endpoints (protected by AdminAuthorizationFilter)
app.MapAdminEndpoints();

app.Run();
