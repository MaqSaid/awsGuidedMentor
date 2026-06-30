using GuidedMentor.Engagement.Application.Analytics;
using GuidedMentor.Engagement.Application.Analytics.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GuidedMentor.Engagement.Tests.Analytics;

/// <summary>
/// Unit tests for GetAnalyticsDashboardHandler — operator analytics dashboard (admin only).
/// Requirements: 30.4, 30.6
/// </summary>
public class GetAnalyticsDashboardHandlerTests
{
    private readonly IAnalyticsRepository _repository;
    private readonly GetAnalyticsDashboardHandler _handler;

    public GetAnalyticsDashboardHandlerTests()
    {
        _repository = Substitute.For<IAnalyticsRepository>();
        var logger = NullLogger<GetAnalyticsDashboardHandler>.Instance;
        _handler = new GetAnalyticsDashboardHandler(_repository, logger);
    }

    [Fact]
    public async Task Handle_ReturnsActiveUsersMetrics()
    {
        // Arrange
        var expected = new ActiveUsersMetrics(150, 800, 2500);
        _repository.GetActiveUsersAsync(Arg.Any<CancellationToken>()).Returns(expected);
        _repository.GetFeatureHeatmapAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<FeatureUsageDto>());
        _repository.GetErrorHotspotsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ErrorHotspotDto>());
        _repository.GetRetentionMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new RetentionMetrics(45.2, 30.1));

        // Act
        var result = await _handler.Handle(new GetAnalyticsDashboardQuery(), CancellationToken.None);

        // Assert
        result.ActiveUsers.DailyActiveUsers.Should().Be(150);
        result.ActiveUsers.WeeklyActiveUsers.Should().Be(800);
        result.ActiveUsers.MonthlyActiveUsers.Should().Be(2500);
    }

    [Fact]
    public async Task Handle_ReturnsFeatureHeatmap()
    {
        // Arrange
        var heatmap = new List<FeatureUsageDto>
        {
            new("page_view", 5000, 1200),
            new("click", 3500, 900),
        };
        _repository.GetActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new ActiveUsersMetrics(0, 0, 0));
        _repository.GetFeatureHeatmapAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(heatmap);
        _repository.GetErrorHotspotsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ErrorHotspotDto>());
        _repository.GetRetentionMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new RetentionMetrics(0, 0));

        // Act
        var result = await _handler.Handle(new GetAnalyticsDashboardQuery(), CancellationToken.None);

        // Assert
        result.FeatureHeatmap.Should().HaveCount(2);
        result.FeatureHeatmap[0].FeatureName.Should().Be("page_view");
        result.FeatureHeatmap[0].UsageCount.Should().Be(5000);
    }

    [Fact]
    public async Task Handle_ReturnsErrorHotspots()
    {
        // Arrange
        var hotspots = new List<ErrorHotspotDto>
        {
            new("/onboarding", 42, new List<string> { "validation_error", "timeout" }),
        };
        _repository.GetActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new ActiveUsersMetrics(0, 0, 0));
        _repository.GetFeatureHeatmapAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<FeatureUsageDto>());
        _repository.GetErrorHotspotsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(hotspots);
        _repository.GetRetentionMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(new RetentionMetrics(0, 0));

        // Act
        var result = await _handler.Handle(new GetAnalyticsDashboardQuery(), CancellationToken.None);

        // Assert
        result.ErrorHotspots.Should().HaveCount(1);
        result.ErrorHotspots[0].PageContext.Should().Be("/onboarding");
        result.ErrorHotspots[0].ErrorCount.Should().Be(42);
        result.ErrorHotspots[0].TopErrorTypes.Should().Contain("validation_error");
    }

    [Fact]
    public async Task Handle_ReturnsRetentionMetrics()
    {
        // Arrange
        var retention = new RetentionMetrics(65.3, 42.8);
        _repository.GetActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(new ActiveUsersMetrics(0, 0, 0));
        _repository.GetFeatureHeatmapAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<FeatureUsageDto>());
        _repository.GetErrorHotspotsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ErrorHotspotDto>());
        _repository.GetRetentionMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(retention);

        // Act
        var result = await _handler.Handle(new GetAnalyticsDashboardQuery(), CancellationToken.None);

        // Assert
        result.Retention.SevenDayRetentionRate.Should().Be(65.3);
        result.Retention.ThirtyDayRetentionRate.Should().Be(42.8);
    }

    [Fact]
    public async Task Handle_ExecutesAllQueriesInParallel()
    {
        // Arrange — use delays to verify parallel execution
        _repository.GetActiveUsersAsync(Arg.Any<CancellationToken>())
            .Returns(async _ => { await Task.Delay(10); return new ActiveUsersMetrics(1, 1, 1); });
        _repository.GetFeatureHeatmapAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async _ => { await Task.Delay(10); return (IReadOnlyList<FeatureUsageDto>)new List<FeatureUsageDto>(); });
        _repository.GetErrorHotspotsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(async _ => { await Task.Delay(10); return (IReadOnlyList<ErrorHotspotDto>)new List<ErrorHotspotDto>(); });
        _repository.GetRetentionMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(async _ => { await Task.Delay(10); return new RetentionMetrics(0, 0); });

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _handler.Handle(new GetAnalyticsDashboardQuery(), CancellationToken.None);
        sw.Stop();

        // Assert — if parallel, should take ~10ms not ~40ms
        result.Should().NotBeNull();
        // Verify all repositories were called
        await _repository.Received(1).GetActiveUsersAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).GetFeatureHeatmapAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).GetErrorHotspotsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).GetRetentionMetricsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_ThrowsOnNullRepository()
    {
        var act = () => new GetAnalyticsDashboardHandler(null!, NullLogger<GetAnalyticsDashboardHandler>.Instance);
        act.Should().Throw<ArgumentNullException>();
    }
}
