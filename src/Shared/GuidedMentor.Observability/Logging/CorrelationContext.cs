namespace GuidedMentor.Observability.Logging;

/// <summary>
/// Provides async-local storage for correlation ID and user ID,
/// allowing enrichers and middleware to share request-scoped context
/// without direct HttpContext dependency.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();
    private static readonly AsyncLocal<string?> _userId = new();

    /// <summary>
    /// Gets or sets the correlation ID for the current async execution context.
    /// </summary>
    public static string? CurrentCorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    /// <summary>
    /// Gets or sets the user ID for the current async execution context.
    /// </summary>
    public static string? CurrentUserId
    {
        get => _userId.Value;
        set => _userId.Value = value;
    }
}
