using GuidedMentor.Engagement.Application.Configuration;
using GuidedMentor.Engagement.Application.Plugins;
using GuidedMentor.Engagement.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GuidedMentor.Engagement.Application;

/// <summary>
/// Registers Engagement AI Help Assistant services into the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers AI Help Assistant plugin, rate limiter, and Bedrock Guardrails configuration.
    /// Requires that IChatClient is already registered by the Infrastructure layer.
    /// </summary>
    public static IServiceCollection AddEngagementHelpAssistant(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // AI Help Assistant plugin (scoped per request, uses IChatClient from infrastructure)
        services.AddScoped<HelpAssistantPlugin>();

        // Rate limiter (singleton — maintains per-user windows across requests)
        services.AddSingleton<IChatRateLimiter, ChatRateLimiter>();

        // TimeProvider for testable time (if not already registered)
        services.TryAddSingleton(TimeProvider.System);

        // Bedrock Guardrails configuration
        services.Configure<BedrockGuardrailsOptions>(
            configuration.GetSection(BedrockGuardrailsOptions.SectionName));

        return services;
    }
}
