namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Command to ingest a batch of tracked engagement events from the frontend.
///
/// Requirements: 30.2, 30.3
/// </summary>
public sealed record IngestEventsCommand(
    Guid UserId,
    IReadOnlyList<IngestEventDto> Events);

public sealed record IngestEventDto(
    string EventType,
    Dictionary<string, object>? EventData,
    long Timestamp,
    string SessionId,
    string PageContext,
    string ActiveRole);
