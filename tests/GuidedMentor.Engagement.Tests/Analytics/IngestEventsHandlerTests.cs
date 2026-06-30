using GuidedMentor.Engagement.Application.Analytics;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;

namespace GuidedMentor.Engagement.Tests.Analytics;

/// <summary>
/// Unit tests for IngestEventsHandler — batch persist to EngagementEvents_Table with hashed userId.
/// Requirements: 30.2, 30.3, 30.11
/// </summary>
public class IngestEventsHandlerTests
{
    private readonly FakeEngagementEventRepository _repository = new();
    private readonly IngestEventsHandler _handler;

    public IngestEventsHandlerTests()
    {
        _handler = new IngestEventsHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_WithValidEvents_PersistsBatchToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var events = new List<IngestEventDto>
        {
            new("page_view", new Dictionary<string, object> { ["pageName"] = "browse" }, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Guid.NewGuid().ToString(), "/browse", "mentee"),
            new("click", new Dictionary<string, object> { ["element"] = "mentor-card" }, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Guid.NewGuid().ToString(), "/browse", "mentee"),
        };
        var command = new IngestEventsCommand(userId, events);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.PersistedEvents.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_HashesUserIdWithSha256()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var events = new List<IngestEventDto>
        {
            new("page_view", null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Guid.NewGuid().ToString(), "/dashboard", "mentor"),
        };
        var command = new IngestEventsCommand(userId, events);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        var persisted = _repository.PersistedEvents[0];
        persisted.UserIdHash.Should().NotBe(userId.ToString());
        persisted.UserIdHash.Should().HaveLength(64); // SHA-256 hex = 64 chars
        persisted.UserIdHash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task HandleAsync_SameUserIdAlwaysProducesSameHash()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hash1 = IngestEventsHandler.HashUserId(userId);
        var hash2 = IngestEventsHandler.HashUserId(userId);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task HandleAsync_TagsEventsWithActiveRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var events = new List<IngestEventDto>
        {
            new("click", null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Guid.NewGuid().ToString(), "/sessions", "mentor"),
        };
        var command = new IngestEventsCommand(userId, events);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _repository.PersistedEvents[0].ActiveRole.Should().Be("mentor");
    }

    [Fact]
    public async Task HandleAsync_WithEmptyEvents_ReturnsFailure()
    {
        // Arrange
        var command = new IngestEventsCommand(Guid.NewGuid(), new List<IngestEventDto>());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No events");
    }

    [Fact]
    public async Task HandleAsync_SetsTtlTo90Days()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var events = new List<IngestEventDto>
        {
            new("page_view", null, timestamp, Guid.NewGuid().ToString(), "/", "mentee"),
        };
        var command = new IngestEventsCommand(userId, events);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        var persisted = _repository.PersistedEvents[0];
        var expectedTtl = (timestamp / 1000) + (90 * 24 * 60 * 60);
        persisted.Ttl.Should().Be(expectedTtl);
    }

    /// <summary>
    /// In-memory fake repository for testing.
    /// </summary>
    private sealed class FakeEngagementEventRepository : IEngagementEventRepository
    {
        public List<EngagementEvent> PersistedEvents { get; } = new();

        public Task BatchPutAsync(IReadOnlyList<EngagementEvent> events, CancellationToken ct = default)
        {
            PersistedEvents.AddRange(events);
            return Task.CompletedTask;
        }
    }
}
