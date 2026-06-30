namespace GuidedMentor.Engagement.Application.Services;

/// <summary>
/// Abstraction for per-user rate limiting on the AI Help Assistant chat.
/// </summary>
public interface IChatRateLimiter
{
    /// <summary>
    /// Checks whether the specified user is allowed to send a message.
    /// Returns true if within rate limit, false if exceeded.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>True if the message is allowed; false if rate limited.</returns>
    bool IsAllowed(Guid userId);

    /// <summary>
    /// Gets the number of remaining messages the user can send in the current window.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>Number of remaining messages (0 if rate limited).</returns>
    int GetRemainingMessages(Guid userId);
}
