using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Domain.Entities;

/// <summary>
/// Represents a tracked user engagement event persisted to the EngagementEvents_Table.
/// UserId is SHA-256 hashed for privacy.
///
/// Requirements: 30.1, 30.2, 30.11
/// </summary>
public sealed class EngagementEvent : Entity<Guid>
{
    public string UserIdHash { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public Dictionary<string, object>? EventData { get; private set; }
    public long Timestamp { get; private set; }
    public string SessionId { get; private set; } = string.Empty;
    public string PageContext { get; private set; } = string.Empty;
    public string ActiveRole { get; private set; } = string.Empty;
    public long Ttl { get; private set; }

    private EngagementEvent() { }

    public static EngagementEvent Create(
        string userIdHash,
        string eventType,
        Dictionary<string, object>? eventData,
        long timestamp,
        string sessionId,
        string pageContext,
        string activeRole)
    {
        if (string.IsNullOrWhiteSpace(userIdHash))
            throw new ArgumentException("UserIdHash cannot be empty.", nameof(userIdHash));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType cannot be empty.", nameof(eventType));
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));
        if (activeRole is not ("mentor" or "mentee"))
            throw new ArgumentException("ActiveRole must be 'mentor' or 'mentee'.", nameof(activeRole));

        var entity = new EngagementEvent
        {
            Id = Guid.NewGuid(),
            UserIdHash = userIdHash,
            EventType = eventType,
            EventData = eventData,
            Timestamp = timestamp,
            SessionId = sessionId,
            PageContext = pageContext,
            ActiveRole = activeRole,
            // TTL = 90 days from event timestamp (in epoch seconds)
            Ttl = (timestamp / 1000) + (90 * 24 * 60 * 60),
        };

        return entity;
    }
}
