namespace GuidedMentor.SharedInfrastructure.ErrorHandling.Exceptions;

/// <summary>
/// Thrown when the user or system has exceeded a rate limit (e.g., AI inference throttling).
/// Maps to HTTP 429 Too Many Requests.
/// </summary>
public sealed class RateLimitedException : Exception
{
    public RateLimitedException()
        : base("Too many requests. Please try again later.")
    {
    }

    public RateLimitedException(string message)
        : base(message)
    {
    }

    public RateLimitedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Optional number of seconds the client should wait before retrying.
    /// </summary>
    public int? RetryAfterSeconds { get; init; }
}
