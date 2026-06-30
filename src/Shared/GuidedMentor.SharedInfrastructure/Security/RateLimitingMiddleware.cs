using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuidedMentor.SharedInfrastructure.Security;

/// <summary>
/// Middleware that enforces per-user rate limiting using a sliding window algorithm.
/// Default: 100 requests per minute per authenticated user.
/// Returns 429 Too Many Requests with Retry-After header when limit is exceeded.
/// 
/// Uses an in-memory sliding window counter. For multi-instance deployments,
/// consider replacing with a DynamoDB-backed or ElastiCache-backed implementation.
/// </summary>
public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _counters = new();

    // Cleanup timer to prevent memory leaks from stale entries
    private readonly Timer _cleanupTimer;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<SecurityOptions> options,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;

        // Clean up expired entries every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for unauthenticated requests (they'll be rejected by JWT middleware)
        var userId = context.Items["UserId"]?.ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            await _next(context);
            return;
        }

        var window = TimeSpan.FromSeconds(_options.RateLimitWindowSeconds);
        var maxRequests = _options.RateLimitMaxRequests;

        var counter = _counters.GetOrAdd(userId, _ => new SlidingWindowCounter(window, maxRequests));
        var now = DateTimeOffset.UtcNow;

        if (!counter.TryIncrement(now))
        {
            var retryAfterSeconds = counter.GetRetryAfterSeconds(now);

            _logger.LogWarning(
                "Rate limit exceeded for user {UserId}. Limit={MaxRequests}/{WindowSeconds}s RetryAfter={RetryAfterSeconds}s",
                userId,
                maxRequests,
                _options.RateLimitWindowSeconds,
                retryAfterSeconds);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 429,
                error = "RateLimited",
                message = $"Rate limit exceeded. Maximum {maxRequests} requests per {_options.RateLimitWindowSeconds} seconds.",
                correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id?.ToString() : Guid.NewGuid().ToString("D"),
                retryAfterSeconds
            });
            return;
        }

        // Add rate limit headers to response
        var remaining = counter.GetRemainingRequests(now);
        context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = counter.GetWindowResetUnixTimestamp(now).ToString();

        await _next(context);
    }

    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTimeOffset.UtcNow;
        var keysToRemove = _counters
            .Where(kvp => kvp.Value.IsExpired(now))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _counters.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            _logger.LogDebug("Rate limiter cleanup removed {Count} expired entries", keysToRemove.Count);
        }
    }
}

/// <summary>
/// Thread-safe sliding window rate limit counter.
/// Tracks request timestamps within the sliding window and enforces the maximum count.
/// </summary>
internal sealed class SlidingWindowCounter
{
    private readonly TimeSpan _window;
    private readonly int _maxRequests;
    private readonly object _lock = new();
    private readonly Queue<DateTimeOffset> _timestamps = new();

    public SlidingWindowCounter(TimeSpan window, int maxRequests)
    {
        _window = window;
        _maxRequests = maxRequests;
    }

    /// <summary>
    /// Attempts to record a new request. Returns true if within limit, false if exceeded.
    /// </summary>
    public bool TryIncrement(DateTimeOffset now)
    {
        lock (_lock)
        {
            PurgeExpired(now);

            if (_timestamps.Count >= _maxRequests)
            {
                return false;
            }

            _timestamps.Enqueue(now);
            return true;
        }
    }

    /// <summary>
    /// Gets the number of remaining requests in the current window.
    /// </summary>
    public int GetRemainingRequests(DateTimeOffset now)
    {
        lock (_lock)
        {
            PurgeExpired(now);
            return Math.Max(0, _maxRequests - _timestamps.Count);
        }
    }

    /// <summary>
    /// Gets the number of seconds until the oldest request expires from the window.
    /// </summary>
    public int GetRetryAfterSeconds(DateTimeOffset now)
    {
        lock (_lock)
        {
            PurgeExpired(now);

            if (_timestamps.Count == 0)
                return 0;

            var oldest = _timestamps.Peek();
            var expiresAt = oldest + _window;
            var retryAfter = (int)Math.Ceiling((expiresAt - now).TotalSeconds);
            return Math.Max(1, retryAfter);
        }
    }

    /// <summary>
    /// Gets the Unix timestamp when the current window resets.
    /// </summary>
    public long GetWindowResetUnixTimestamp(DateTimeOffset now)
    {
        lock (_lock)
        {
            if (_timestamps.Count == 0)
                return now.ToUnixTimeSeconds() + (long)_window.TotalSeconds;

            var oldest = _timestamps.Peek();
            return (oldest + _window).ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// Returns true if this counter has no requests within the window (safe to remove).
    /// </summary>
    public bool IsExpired(DateTimeOffset now)
    {
        lock (_lock)
        {
            PurgeExpired(now);
            return _timestamps.Count == 0;
        }
    }

    private void PurgeExpired(DateTimeOffset now)
    {
        var windowStart = now - _window;
        while (_timestamps.Count > 0 && _timestamps.Peek() < windowStart)
        {
            _timestamps.Dequeue();
        }
    }
}
