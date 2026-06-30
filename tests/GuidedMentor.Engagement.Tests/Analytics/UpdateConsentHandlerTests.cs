using GuidedMentor.Engagement.Application.Analytics;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;

namespace GuidedMentor.Engagement.Tests.Analytics;

/// <summary>
/// Unit tests for UpdateConsentHandler — persist user consent preference.
/// Requirements: 30.7, 30.8
/// </summary>
public class UpdateConsentHandlerTests
{
    private readonly FakeConsentRepository _repository = new();
    private readonly UpdateConsentHandler _handler;

    public UpdateConsentHandlerTests()
    {
        _handler = new UpdateConsentHandler(_repository);
    }

    [Theory]
    [InlineData("granted")]
    [InlineData("denied")]
    public async Task HandleAsync_WithValidStatus_PersistsPreference(string consent)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateConsentCommand(userId, consent);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.Store.Should().ContainKey(userId);
        _repository.Store[userId].Status.Should().Be(consent);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidStatus_ReturnsFailure()
    {
        // Arrange
        var command = new UpdateConsentCommand(Guid.NewGuid(), "maybe");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("granted");
    }

    [Fact]
    public async Task HandleAsync_UpdatesExistingPreference()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var initial = ConsentPreference.Create(userId, "granted");
        _repository.Store[userId] = initial;

        var command = new UpdateConsentCommand(userId, "denied");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.Store[userId].Status.Should().Be("denied");
    }

    [Fact]
    public async Task HandleAsync_CreatesNewPreferenceIfNoneExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UpdateConsentCommand(userId, "granted");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.Store.Should().ContainKey(userId);
    }

    /// <summary>
    /// In-memory fake consent repository for testing.
    /// </summary>
    private sealed class FakeConsentRepository : IConsentRepository
    {
        public Dictionary<Guid, ConsentPreference> Store { get; } = new();

        public Task<ConsentPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            Store.TryGetValue(userId, out var preference);
            return Task.FromResult(preference);
        }

        public Task UpsertAsync(ConsentPreference consent, CancellationToken ct = default)
        {
            Store[consent.UserId] = consent;
            return Task.CompletedTask;
        }
    }
}
