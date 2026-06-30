using System.Reflection;
using FluentValidation;
using GuidedMentor.SharedInfrastructure.AuditLogging;
using GuidedMentor.SharedInfrastructure.Behaviors;
using GuidedMentor.SharedInfrastructure.ErrorHandling;
using GuidedMentor.SharedInfrastructure.Resilience;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GuidedMentor.SharedInfrastructure;

/// <summary>
/// Extension methods for registering shared infrastructure services
/// (MediatR behaviors, FluentValidation, Polly resilience pipelines, global exception handling).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers MediatR pipeline behaviors, Polly v8 resilience pipelines,
    /// FluentValidation auto-discovery, and the global exception handler from the calling assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">
    /// Assemblies to scan for MediatR handlers and FluentValidation validators.
    /// If empty, uses the calling assembly.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGuidedMentorInfrastructure(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var assembliesToScan = assemblies.Length > 0
            ? assemblies
            : [Assembly.GetCallingAssembly()];

        // Register MediatR with pipeline behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assembliesToScan);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuditLoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        // Register FluentValidation auto-discovery from provided assemblies
        services.AddValidatorsFromAssemblies(assembliesToScan, includeInternalTypes: true);

        // Register Polly v8 resilience pipelines (bedrock, dynamodb, aurora)
        services.AddGuidedMentorResilience();

        // Register audit log writer (Serilog → CloudWatch "audit-log" log group)
        services.AddSingleton<IAuditLogWriter, CloudWatchAuditLogWriter>();

        // Register global exception handler
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
