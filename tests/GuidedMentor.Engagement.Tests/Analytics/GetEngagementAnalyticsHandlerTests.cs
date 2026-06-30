using GuidedMentor.Engagement.Application.Analytics;
using GuidedMentor.Engagement.Application.Analytics.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GuidedMentor.Engagement.Tests.Analytics;

/// <summary>
/// Unit tests for GetEngagementAnalyticsHandler — engagement-specific analytics (admin only).
/// Requirements: 30.5
/// </summary>
public class GetEngagementAnalyticsHandlerTests
{
    private readonly IAnalyticsRepository _repository;
    private readonly GetEngagementAnalyticsHandler _handler;

    public GetEngagementAnalyticsHandlerTests()
    {
        _repository = Substitute.For<IAnalyticsRepository>();
        var logger = NullLogger<GetEngagementAnalyticsHandler>.Instance;
        _handler = new GetEngagementAnalyticsHandler(_repository, logger);
    }

    [Fact]
    public async Task Handle_ReturnsBrowseToLockConversionRate()
    {
        // Arrange
        var analytics = new EngagementAnalyticsDto(
            BrowseToLockConversionRate: 12.5,
            PlanToCompletionRate: 45.0,
            JobViewToClickRate: 8.3,
            DailyBreakdown: new List<EngagementMetricBreakdownDto>());
        _repository.GetEngagementAnalyticsAsync(
            Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(analytics);

        // Act
        var result = await _handler.Handle(
            new GetEngagementAnalyticsQuery(), CancellationToken.None);

        // Assert
        result.BrowseToLockConversionRate.Should().Be(12.5);
    }

    [Fact]
    public async Task Handle_ReturnsPlanToCompletionRate()
    {
        // Arrange
        var analytics = new EngagementAnalyticsDto(35.0, 72.5, 15.0,
            new List<EngagementMetricBreakdownDto>());
        _repository.GetEngagementAnalyticsAsync(
            Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(analytics);

        // Act
        var result = await _handler.Handle(
            new GetEngagementAnalyticsQuery(), CancellationToken.None);

        // Assert
        result.PlanToCompletionRate.Should().Be(72.5);
    }

    [Fact]
    public async Task Handle_ReturnsJobViewToClickRate()
    {
        // Arrange
        var analytics = new EngagementAnalyticsDto(20.0, 50.0, 5.7,
            new List<EngagementMetricBreakdownDto>());
        _repository.GetEngagementAnalyticsAsync(
            Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(analytics);

        // Act
        var result = await _handler.Handle(
            new GetEngagementAnalyticsQuery(), CancellationToken.None);

        // Assert
        result.JobViewToClickRate.Should().Be(5.7);
    }

    [Fact]
    public async Task Handle_PassesDateRangeToRepository()
    {
        // Arrange
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 1, 31);
        _repository.GetEngagementAnalyticsAsync(
            Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new EngagementAnalyticsDto(0, 0, 0, new List<EngagementMetricBreakdownDto>()));

        // Act
        await _handler.Handle(new GetEngagementAnalyticsQuery(from, to), CancellationToken.None);

        // Assert
        await _repository.Received(1).GetEngagementAnalyticsAsync(
            from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsDailyBreakdown()
    {
        // Arrange
        var breakdown = new List<EngagementMetricBreakdownDto>
        {
            new(new DateOnly(2024, 1, 1), 100, 12, 50, 30, 80, 6),
            new(new DateOnly(2024, 1, 2), 120, 15, 55, 35, 90, 8),
        };
        var analytics = new EngagementAnalyticsDto(12.0, 60.0, 7.5, breakdown);
        _repository.GetEngagementAnalyticsAsync(
            Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(analytics);

        // Act
        var result = await _handler.Handle(
            new GetEngagementAnalyticsQuery(), CancellationToken.None);

        // Assert
        result.DailyBreakdown.Should().HaveCount(2);
        result.DailyBreakdown[0].Date.Should().Be(new DateOnly(2024, 1, 1));
        result.DailyBreakdown[0].BrowseEvents.Should().Be(100);
        result.DailyBreakdown[0].LockEvents.Should().Be(12);
    }

    [Fact]
    public void Constructor_ThrowsOnNullRepository()
    {
        var act = () => new GetEngagementAnalyticsHandler(null!, NullLogger<GetEngagementAnalyticsHandler>.Instance);
        act.Should().Throw<ArgumentNullException>();
    }
}
