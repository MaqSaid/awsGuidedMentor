using System.Text.Json;
using Amazon.AppConfigData;
using Amazon.AppConfigData.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuidedMentor.SharedInfrastructure.FeatureFlags;

/// <summary>
/// AWS AppConfig-backed feature flag service.
/// Polls AppConfig for feature flag configuration and caches locally.
/// Supports canary deployment percentages (1% → 10% → 50% → 100%).
/// </summary>
public sealed class AppConfigFeatureFlagService : IFeatureFlagService, IDisposable
{
    private readonly IAmazonAppConfigData _client;
    private readonly AppConfigOptions _options;
    private readonly ILogger<AppConfigFeatureFlagService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _sessionToken;
    private Dictionary<string, FeatureFlagState> _flags = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _pollInterval;

    public AppConfigFeatureFlagService(
        IAmazonAppConfigData client,
        IOptions<AppConfigOptions> options,
        ILogger<AppConfigFeatureFlagService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
        _pollInterval = TimeSpan.FromSeconds(_options.PollIntervalSeconds);
    }

    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        await EnsureConfigurationLoadedAsync(cancellationToken);

        if (!_flags.TryGetValue(featureName, out var state))
        {
            _logger.LogWarning("Feature flag '{FeatureName}' not found in AppConfig configuration", featureName);
            return false;
        }

        return state.Enabled && state.RolloutPercentage >= 100;
    }

    public async Task<int?> GetRolloutPercentageAsync(string featureName, CancellationToken cancellationToken = default)
    {
        await EnsureConfigurationLoadedAsync(cancellationToken);

        if (!_flags.TryGetValue(featureName, out var state))
        {
            return null;
        }

        return state.RolloutPercentage;
    }

    private async Task EnsureConfigurationLoadedAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _lastRefresh < _pollInterval && _flags.Count > 0)
        {
            return;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (DateTime.UtcNow - _lastRefresh < _pollInterval && _flags.Count > 0)
            {
                return;
            }

            await RefreshConfigurationAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task RefreshConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Start a new session if we don't have one
            if (_sessionToken is null)
            {
                var startRequest = new StartConfigurationSessionRequest
                {
                    ApplicationIdentifier = _options.ApplicationId,
                    EnvironmentIdentifier = _options.EnvironmentId,
                    ConfigurationProfileIdentifier = _options.ConfigurationProfileId,
                    RequiredMinimumPollIntervalInSeconds = (int)_pollInterval.TotalSeconds,
                };

                var startResponse = await _client.StartConfigurationSessionAsync(startRequest, cancellationToken);
                _sessionToken = startResponse.InitialConfigurationToken;
            }

            // Get latest configuration
            var getRequest = new GetLatestConfigurationRequest
            {
                ConfigurationToken = _sessionToken,
            };

            var response = await _client.GetLatestConfigurationAsync(getRequest, cancellationToken);
            _sessionToken = response.NextPollConfigurationToken;

            // Only parse if configuration has changed (non-empty body)
            if (response.Configuration is { Length: > 0 })
            {
                var json = await JsonSerializer.DeserializeAsync<Dictionary<string, FeatureFlagState>>(
                    response.Configuration,
                    cancellationToken: cancellationToken);

                if (json is not null)
                {
                    _flags = json;
                    _logger.LogInformation(
                        "Refreshed {Count} feature flags from AppConfig",
                        _flags.Count);
                }
            }

            _lastRefresh = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh feature flags from AppConfig. Using cached values.");

            // Reset session token on error to force new session on next attempt
            if (ex is BadRequestException or ResourceNotFoundException)
            {
                _sessionToken = null;
            }

            // If we have cached flags, continue using them
            if (_flags.Count > 0)
            {
                _lastRefresh = DateTime.UtcNow;
            }
        }
    }

    public void Dispose()
    {
        _refreshLock.Dispose();
    }
}

/// <summary>
/// Represents the state of a single feature flag in AppConfig.
/// </summary>
internal sealed class FeatureFlagState
{
    /// <summary>Whether the feature is enabled at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Rollout percentage for canary deployments (0-100).
    /// Progression: 1% → 10% → 50% → 100%.
    /// </summary>
    public int RolloutPercentage { get; set; }
}
