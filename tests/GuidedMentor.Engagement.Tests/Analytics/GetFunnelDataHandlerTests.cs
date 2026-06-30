using GuidedMentor.Engagement.Application.Analytics;
using GuidedMentor.Engagement.Application.Analytics.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GuidedMentor.Engagement.Tests.Analytics;

/// <summary>
/// Unit tests for GetFunnelDataHandler — conversion funnel analysis (admin only).
/// Requirements: 30.6
/// </summary>
public class GetFunnelDataHandlerTests
{
    private readonly IAnalyticsRepository _repository;
    private readonly GetFunnelDataHandler _handler;

    public GetFunnelDataHandlerTests()
    {
        _repository = Substitute.For<IAnalyticsRepository>();
        var logger = NullLogger<GetFunnelDataHandler>.Instance;
        _handler = new GetFunnelDataHandler(_repository, logger);
    }

    [Fact]
    public async Task Handle_ReturnsFunnelDataFromRepository()
    {
        // Arrange
        var stages = new List<FunnelStageDto>
        {
            new("Signup", 1000, 100.0, 0.0),
            new("Onboard", 800, 80.0, 20.0),
            new("Browse", 600, 75.0, 25.0),
            new("Match", 300, 50.0, 50.0),
            new("Session", 200, 66.7, 33.3),
            new("Complete", 150, 75.0, 25.0),
        };
        var funnelData = new FunnelDataDto(stages, 15.0);
        _repository.GetFunnelDataAsync(Arg.Any<CancellationToken>()).Returns(funnelData);

        // Act
        var result = await _handler.Handle(new GetFunnelDataQuery(), CancellationToken.None);

        // Assert
        result.Stages.Should().HaveCount(6);
        result.OverallConversionRate.Should().Be(15.0);
    }

    [Fact]
    public async Task Handle_FunnelStagesFollowExpectedOrder()
    {
        // Arrange
        var stages = new List<FunnelStageDto>
        {
            new("Signup", 500, 100.0, 0.0),
            new("Onboard", 400, 80.0, 20.0),
            new("Browse", 350, 87.5, 12.5),
            new("Match", 200, 57.1, 42.9),
            new("Session", 150, 75.0, 25.0),
            new("Complete", 100, 66.7, 33.3),
        };
        _repository.GetFunnelDataAsync(Arg.Any<CancellationToken>())
            .Returns(new FunnelDataDto(stages, 20.0));

        // Act
        var result = await _handler.Handle(new GetFunnelDataQuery(), CancellationToken.None);

        // Assert
        var stageNames = result.Stages.Select(s => s.StageName).ToList();
        stageNames.Should().ContainInOrder("Signup", "Onboard", "Browse", "Match", "Session", "Complete");
    }

    [Fact]
    public async Task Handle_WithEmptyFunnel_ReturnsZeroConversion()
    {
        // Arrange
        _repository.GetFunnelDataAsync(Arg.Any<CancellationToken>())
            .Returns(new FunnelDataDto(new List<FunnelStageDto>(), 0));

        // Act
        var result = await _handler.Handle(new GetFunnelDataQuery(), CancellationToken.None);

        // Assert
        result.Stages.Should().BeEmpty();
        result.OverallConversionRate.Should().Be(0);
    }

    [Fact]
    public void Constructor_ThrowsOnNullRepository()
    {
        var act = () => new GetFunnelDataHandler(null!, NullLogger<GetFunnelDataHandler>.Instance);
        act.Should().Throw<ArgumentNullException>();
    }
}
