using System.Collections.Concurrent;

namespace GuidedMentor.Engagement.Application.Services;

/// <summary>
/// Per-user rate limiter for the AI Help Assistant chat.
/// Enforces a maximum of 20 messages per minute per user using a sliding window approach.
/// 
/// Validates: Requirements 14.11
/// </summary>
public sealed class ChatRateLimiter : IChatRateLimiter
{
    /// <summary>
    /// Maximum number of messages allowed per user within the time window.
    /// </summary>
    public const int MaxMessagesPerWindow = 20;

    /// <summary>
    /// The sliding window duration (1 minute).
    /// </summary>
    public static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<Guid, UserMessageWindow> _userWindows = new();
    private readonly TimeProvider _timeProvider;

    public ChatRateLimiter(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Checks whether the specified user is allowed to send a message.
    /// Returns true if within rate limit, false if exceeded.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>True if the message is allowed; false if rate limited.</returns>
    public bool IsAllowed(Guid userId)
    {
        var now = _timeProvider.GetUtcNow();
        var window = _userWindows.GetOrAdd(userId, _ => new UserMessageWindow());

        return window.TryRecord(now, MaxMessagesPerWindow, WindowDuration);
    }

    /// <summary>
    /// Gets the number of remaining messages the user can send in the current window.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>Number of remaining messages (0 if rate limited).</returns>
    public int GetRemainingMessages(Guid userId)
    {
        var now = _timeProvider.GetUtcNow();

        if (!_userWindows.TryGetValue(userId, out var window))
            return MaxMessagesPerWindow;

        return window.GetRemaining(now, MaxMessagesPerWindow, WindowDuration);
    }

    /// <summary>
    /// Tracks message timestamps for a single user using a sliding window.
    /// Thread-safe via lock on the internal queue.
    /// </summary>
    internal sealed class UserMessageWindow
    {
        private readonly Queue<DateTimeOffset> _timestamps = new();
        private readonly object _lock = new();

        /// <summary>
        /// Attempts to record a new message. Removes expired timestamps from the window,
        /// then checks if the user is still below the limit.
        /// </summary>
        public bool TryRecord(DateTimeOffset now, int maxMessages, TimeSpan windowDuration)
        {
            lock (_lock)
            {
                PruneExpired(now, windowDuration);

                if (_timestamps.Count >= maxMessages)
                    return false;

                _timestamps.Enqueue(now);
                return true;
            }
        }

        /// <summary>
        /// Gets remaining messages without recording a new one.
        /// </summary>
        public int GetRemaining(DateTimeOffset now, int maxMessages, TimeSpan windowDuration)
        {
            lock (_lock)
            {
                PruneExpired(now, windowDuration);
                return Math.Max(0, maxMessages - _timestamps.Count);
            }
        }

        private void PruneExpired(DateTimeOffset now, TimeSpan windowDuration)
        {
            var cutoff = now - windowDuration;

            while (_timestamps.Count > 0 && _timestamps.Peek() < cutoff)
            {
                _timestamps.Dequeue();
            }
        }
    }
}
