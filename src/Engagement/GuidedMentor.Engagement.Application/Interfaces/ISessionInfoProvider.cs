namespace GuidedMentor.Engagement.Application.Interfaces;

/// <summary>
/// Provides session information from the Mentoring bounded context.
/// Used for cross-context queries (e.g., 24-hour meetup reminders).
/// </summary>
public interface ISessionInfoProvider
{
    /// <summary>
    /// Gets basic session info needed for meetup reminder notifications.
    /// </summary>
    Task<SessionInfo?> GetSessionInfoAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal session info needed for meetup-aligned notifications.
/// </summary>
public sealed record SessionInfo(
    Guid SessionId,
    Guid MentorId,
    Guid MenteeId,
    string Title);
