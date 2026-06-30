using System.Security.Cryptography;
using System.Text;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Handles batch ingestion of engagement events from the frontend EventTracker.
/// Hashes userId with SHA-256 for privacy before persisting to EngagementEvents_Table.
///
/// Requirements: 30.2, 30.3, 30.11
/// </summary>
public sealed class IngestEventsHandler
{
    private readonly IEngagementEventRepository _repository;

    public IngestEventsHandler(IEngagementEventRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result> HandleAsync(IngestEventsCommand command, CancellationToken ct = default)
    {
        if (command.Events.Count == 0)
            return Result.Failure("No events to ingest.");

        var userIdHash = HashUserId(command.UserId);

        var events = new List<EngagementEvent>(command.Events.Count);

        foreach (var dto in command.Events)
        {
            var engagementEvent = EngagementEvent.Create(
                userIdHash: userIdHash,
                eventType: dto.EventType,
                eventData: dto.EventData,
                timestamp: dto.Timestamp,
                sessionId: dto.SessionId,
                pageContext: dto.PageContext,
                activeRole: dto.ActiveRole);

            events.Add(engagementEvent);
        }

        await _repository.BatchPutAsync(events, ct);

        return Result.Success();
    }

    /// <summary>
    /// Hash the userId using SHA-256 for privacy (Requirement 30.2).
    /// </summary>
    internal static string HashUserId(Guid userId)
    {
        var bytes = Encoding.UTF8.GetBytes(userId.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
