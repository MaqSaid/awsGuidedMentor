using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Application.Services;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.Engagement.Infrastructure.Persistence;
using GuidedMentor.Engagement.Infrastructure.RealTime;
using GuidedMentor.Engagement.Infrastructure.Repositories;
using GuidedMentor.Engagement.Infrastructure.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GuidedMentor.Engagement.Infrastructure;

/// <summary>
/// Registers Engagement infrastructure services into the DI container.
/// Refactored: DynamoDB → PostgreSQL, AppSync → SignalR (placeholder), Bedrock removed.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEngagementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL analytics — uses same connection as main DB now
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddSingleton(NpgsqlDataSource.Create(connectionString));
            services.AddScoped<IAnalyticsRepository, AuroraAnalyticsRepository>();
        }

        // Repositories (PostgreSQL-backed)
        services.AddScoped<INotificationRepository, PostgresNotificationRepository>();
        services.AddScoped<IConsentRepository, PostgresConsentRepository>();
        services.AddScoped<IEngagementEventRepository, PostgresEngagementEventRepository>();

        // Real-time notification publisher (no-op placeholder, will become SignalR)
        services.AddScoped<IAppSyncNotificationPublisher, NoOpNotificationPublisher>();

        // HuggingFace options (kept for interface compat — mock in local dev)
        services.Configure<HuggingFaceOptions>(
            configuration.GetSection(HuggingFaceOptions.SectionName));

        // Intent classifier — will be overridden by mock in local dev
        services.AddHttpClient<IIntentClassifier, HuggingFaceIntentClassifier>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<HuggingFaceOptions>>().Value;
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}
