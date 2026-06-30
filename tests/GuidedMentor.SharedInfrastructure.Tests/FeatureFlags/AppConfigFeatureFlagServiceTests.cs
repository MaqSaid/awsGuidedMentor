using System.Text;
using System.Text.Json;
using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;
using GuidedMentor.SharedInfrastructure.FeatureFlags;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GuidedMentor.SharedInfrastructure.Tests.FeatureFlags;

public sealed class AppConfigFeatureFlagServiceTests : IDisposable
{
    private readonly IAmazonAppConfigData _client = Substitute.For<IAmazonAppConfigData>();
    private readonly ILogger<AppConfigFeatureFlagService> _logger = Substitute.For<ILogger<AppConfigFeatureFlagService>>();
    private readonly AppConfigOptions _options = new()
    {
        ApplicationId = "test-app",
        EnvironmentId = "test-env",
        ConfigurationProfileId = "test-profile",
        PollIntervalSeconds = 0 // Disable caching for tests
    };

    private AppConfigFeatureFlagService CreateSut() =>
        new(_client, Options.Create(_options), _logger);

    private void SetupClientWithFlags(Dictionary<string, object> flags)
    {
        _client.StartConfigurationSessionAsync(
                Arg.Any<StartConfigurationSessionRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new StartConfigurationSessionResponse
            {
                InitialConfigurationToken = "token-1"
            });

        var json = JsonSerializer.Serialize(flags);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        _client.GetLatestConfigurationAsync(
                Arg.Any<GetLatestConfigurationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GetLatestConfigurationResponse
            {
                Configuration = stream,
                NextPollConfigurationToken = "token-2"
            });
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFeatureEnabledAt100Percent_ReturnsTrue()
    {
        // Arrange
        SetupClientWithFlags(new Dictionary<string, object>
        {
            ["AiHelp"] = new { Enabled = true, RolloutPercentage = 100 }
        });
        using var sut = CreateSut();

        // Act
        var result = await sut.IsEnabledAsync("AiHelp");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFeatureDisabled_ReturnsFalse()
    {
        // Arrange
        SetupClientWithFlags(new Dictionary<string, object>
        {
            ["AiHelp"] = new { Enabled = false, RolloutPercentage = 100 }
        });
        using var sut = CreateSut();

        // Act
        var result = await sut.IsEnabledAsync("AiHelp");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFeatureNotFound_ReturnsFalse()
    {
        // Arrange
        SetupClientWithFlags(new Dictionary<string, object>());
        using var sut = CreateSut();

        // Act
        var result = await sut.IsEnabledAsync("NonExistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFeatureEnabledButPartialRollout_ReturnsFalse()
    {
        // Arrange — feature enabled but only at 50%, IsEnabled requires 100%
        SetupClientWithFlags(new Dictionary<string, object>
        {
            ["JobBoard"] = new { Enabled = true, RolloutPercentage = 50 }
        });
        using var sut = CreateSut();

        // Act
        var result = await sut.IsEnabledAsync("JobBoard");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetRolloutPercentageAsync_ReturnsConfiguredPercentage()
    {
        // Arrange
        SetupClientWithFlags(new Dictionary<string, object>
        {
            ["SessionPlans"] = new { Enabled = true, RolloutPercentage = 10 }
        });
        using var sut = CreateSut();

        // Act
        var result = await sut.GetRolloutPercentageAsync("SessionPlans");

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task GetRolloutPercentageAsync_WhenFeatureNotFound_ReturnsNull()
    {
        // Arrange
        SetupClientWithFlags(new Dictionary<string, object>());
        using var sut = CreateSut();

        // Act
        var result = await sut.GetRolloutPercentageAsync("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenClientThrows_ReturnsFalse()
    {
        // Arrange
        _client.StartConfigurationSessionAsync(
                Arg.Any<StartConfigurationSessionRequest>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new Amazon.AppConfigData.Model.InternalServerException("Error"));
        using var sut = CreateSut();

        // Act
        var result = await sut.IsEnabledAsync("AiHelp");

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        // No-op for test cleanup
    }
}
