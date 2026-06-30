using Serilog.Core;
using Serilog.Events;

namespace GuidedMentor.Observability.Logging;

/// <summary>
/// Enriches log events with the current user ID from the async-local context.
/// The user ID is expected to be set after JWT validation in the request pipeline.
/// </summary>
public sealed class UserIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var userId = CorrelationContext.CurrentUserId;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var property = propertyFactory.CreateProperty("UserId", userId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
