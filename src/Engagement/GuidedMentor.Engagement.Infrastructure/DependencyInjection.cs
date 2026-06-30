using Amazon.BedrockRuntime;
using Amazon.DynamoDBv2;
using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.Engagement.Infrastructure.AI;
using GuidedMentor.Engagement.Infrastructure.Persistence;
using GuidedMentor.Engagement.Infrastructure.RealTime;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GuidedMentor.Engagement.Infrastructure;

/// <summary>
/// Registers Engagement infrastructure services into the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddEngagementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DynamoDB client (shared)
        services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();

        // Bedrock Runtime client for AI Help Assistant
        services.AddSingleton<IAmazonBedrockRuntime, AmazonBedrockRuntimeClient>();

        // IChatClient abstraction backed by Bedrock Converse API (Claude Sonnet 4)
        services.AddScoped<IChatClient, BedrockChatClient>();

        // Table options
        services.Configure<NotificationTableOptions>(
            configuration.GetSection(NotificationTableOptions.SectionName));

        // AppSync options
        services.Configure<AppSyncOptions>(
            configuration.GetSection(AppSyncOptions.SectionName));

        // Aurora PostgreSQL options
        services.Configure<AuroraOptions>(
            configuration.GetSection(AuroraOptions.SectionName));

        // NpgsqlDataSource for Aurora PostgreSQL analytics database (via RDS Proxy)
        var auroraConnectionString = configuration.GetSection(AuroraOptions.SectionName)
            .GetValue<string>(nameof(AuroraOptions.ConnectionString)) ?? string.Empty;
        if (!string.IsNullOrEmpty(auroraConnectionString))
        {
            services.AddSingleton(NpgsqlDataSource.Create(auroraConnectionString));
            services.AddScoped<IAnalyticsRepository, AuroraAnalyticsRepository>();
        }

        // Repositories
        services.AddScoped<INotificationRepository, DynamoDbNotificationRepository>();

        // DynamoDB Streams → Aurora replication handler
        services.AddScoped<DynamoDbStreamReplicationHandler>();

        // AppSync notification publisher (real-time push)
        services.AddHttpClient<IAppSyncNotificationPublisher, AppSyncNotificationPublisher>();

        // AppSync subscription client (WebSocket with exponential backoff reconnection)
        services.AddSingleton<AppSyncSubscriptionClient>();

        return services;
    }
}
