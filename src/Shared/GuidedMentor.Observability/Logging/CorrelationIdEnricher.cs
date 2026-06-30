using Serilog.Core;
using Serilog.Events;

namespace GuidedMentor.Observability.Logging;

/// <summary>
/// Enriches log events with the current correlation ID from the async-local context.
/// The correlation ID is set by <see cref="Middleware.CorrelationIdMiddleware"/>.
/// </summary>
public sealed class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = CorrelationContext.CurrentCorrelationId;

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            var property = propertyFactory.CreateProperty("CorrelationId", correlationId);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
