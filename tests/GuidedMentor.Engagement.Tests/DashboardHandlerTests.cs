using GuidedMentor.Engagement.Application.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Application.Queries.Dashboard;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GuidedMentor.Engagement.Tests;

/// <summary>
/// Unit tests for Mentee and Mentor Dashboard query handlers.
/// Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5, 11.1, 11.2, 11.6, 11.7
/// </summary>
public sealed class DashboardHandlerTests
{
    private readonly IMenteeDashboardDataProvider _mockMenteeProvider;
    private readonly IMentorDashboardDataProvider _mockMentorProvider;
    private readonly ILogger<GetMenteeDashboardHandler> _menteeLogger;
    private readonly ILogger<GetMentorDashboardHandler> _mentorLogger;

    public DashboardHandlerTests()
    {
        _mockMenteeProvider = Substitute.For<IMenteeDashboardDataProvider>();
        _mockMentorProvider = Substitute.For<IMentorDashboardDataProvider>();
        _menteeLogger = NullLogger<GetMenteeDashboardHandler>.Instance;
        _mentorLogger = NullLogger<GetMentorDashboardHandler>.Instance;
    }

    // --- GetMenteeDashboardHandler Tests ---

    [Fact]
    public async Task MenteeDashboard_AllSectionsLoad_ReturnsCompleteData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = new List<ActiveSessionCardDto>
        {
            new(Guid.NewGuid(), "Alice Smith", "AWS Architecture Review", "Read chapter 5", 65)
        };
        var mentors = new List<RecommendedMentorDto>
        {
            new(Guid.NewGuid(), "Bob Jones", "Sydney", 87, new[] { "Lambda", "DynamoDB" }),
            new(Guid.NewGuid(), "Carol Lee", "Melbourne", 82, new[] { "ECS", "CDK" }),
            new(Guid.NewGuid(), "Dan Park", "Brisbane", 76, new[] { "S3", "CloudFront" })
        };
        var stats = new MenteeProgressStatsDto(2, 15, 10, 67);
        var summary = new MenteeSummaryBarDto(2, 1, 1);

        _mockMenteeProvider.GetActiveSessionsAsync(userId, Arg.Any<CancellationToken>()).Returns(sessions);
        _mockMenteeProvider.GetRecommendedMentorsAsync(userId, 3, Arg.Any<CancellationToken>()).Returns(mentors);
        _mockMenteeProvider.GetProgressStatsAsync(userId, Arg.Any<CancellationToken>()).Returns(stats);
        _mockMenteeProvider.GetSummaryBarAsync(userId, Arg.Any<CancellationToken>()).Returns(summary);

        var handler = new GetMenteeDashboardHandler(_mockMenteeProvider, _menteeLogger);

        // Act
        var result = await handler.Handle(new GetMenteeDashboardQuery(userId), CancellationToken.None);

        // Assert
        result.ActiveSessions.Should().HaveCount(1);
        result.ActiveSessions[0].MentorName.Should().Be("Alice Smith");
        result.ActiveSessions[0].ProgressPercentage.Should().Be(65);
        result.RecommendedMentors.Should().HaveCount(3);
        result.Stats.CompletedSessionsCount.Should().Be(2);
        result.Stats.OverallCompletionPercentage.Should().Be(67);
        result.SummaryBar.CompletedSessions.Should().Be(2);
        result.SummaryBar.InProgressSessions.Should().Be(1);
        result.SummaryBar.PendingRequests.Should().Be(1);
        result.SessionsError.Should().BeNull();
        result.RecommendationsError.Should().BeNull();
        result.StatsError.Should().BeNull();
    }

    [Fact]
    public async Task MenteeDashboard_NoActiveSessions_ReturnsEmptyList()
    {
        // Arrange — empty state scenario (Req 10.3)
        var userId = Guid.NewGuid();
        _mockMenteeProvider.GetActiveSessionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActiveSessionCardDto>());
        _mockMenteeProvider.GetRecommendedMentorsAsync(userId, 3, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RecommendedMentorDto>());
        _mockMenteeProvider.GetProgressStatsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MenteeProgressStatsDto(0, 0, 0, 0));
        _mockMenteeProvider.GetSummaryBarAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MenteeSummaryBarDto(0, 0, 0));

        var handler = new GetMenteeDashboardHandler(_mockMenteeProvider, _menteeLogger);

        // Act
        var result = await handler.Handle(new GetMenteeDashboardQuery(userId), CancellationToken.None);

        // Assert — empty sessions list signals empty state to frontend
        result.ActiveSessions.Should().BeEmpty();
        result.SessionsError.Should().BeNull();
    }

    [Fact]
    public async Task MenteeDashboard_SessionsFailOtherSectionsSucceed_PartialResult()
    {
        // Arrange — per-section error recovery (Req 10.5)
        var userId = Guid.NewGuid();
        _mockMenteeProvider.GetActiveSessionsAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DynamoDB timeout"));
        _mockMenteeProvider.GetRecommendedMentorsAsync(userId, 3, Arg.Any<CancellationToken>())
            .Returns(new List<RecommendedMentorDto>
            {
                new(Guid.NewGuid(), "Mentor A", "Perth", 90, new[] { "EC2" })
            });
        _mockMenteeProvider.GetProgressStatsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MenteeProgressStatsDto(1, 5, 3, 60));
        _mockMenteeProvider.GetSummaryBarAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MenteeSummaryBarDto(1, 0, 0));

        var handler = new GetMenteeDashboardHandler(_mockMenteeProvider, _menteeLogger);

        // Act
        var result = await handler.Handle(new GetMenteeDashboardQuery(userId), CancellationToken.None);

        // Assert — sessions section has error, others still loaded
        result.SessionsError.Should().NotBeNull();
        result.SessionsError!.Section.Should().Be("sessions");
        result.SessionsError.CanRetry.Should().BeTrue();
        result.ActiveSessions.Should().BeEmpty();
        result.RecommendedMentors.Should().HaveCount(1);
        result.Stats.CompletedSessionsCount.Should().Be(1);
        result.RecommendationsError.Should().BeNull();
        result.StatsError.Should().BeNull();
    }

    [Fact]
    public async Task MenteeDashboard_RecommendationsFailOtherSectionsSucceed_PartialResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockMenteeProvider.GetActiveSessionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActiveSessionCardDto>());
        _mockMenteeProvider.GetRecommendedMentorsAsync(userId, 3, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Compatibility service unreachable"));
        _mockMenteeProvider.GetProgressStatsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MenteeProgressStatsDto(0, 0, 0, 0));
        _mockMenteeProvider.GetSummaryBarAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MenteeSummaryBarDto(0, 0, 0));

        var handler = new GetMenteeDashboardHandler(_mockMenteeProvider, _menteeLogger);

        // Act
        var result = await handler.Handle(new GetMenteeDashboardQuery(userId), CancellationToken.None);

        // Assert
        result.RecommendationsError.Should().NotBeNull();
        result.RecommendationsError!.Section.Should().Be("recommendations");
        result.RecommendationsError.CanRetry.Should().BeTrue();
        result.RecommendedMentors.Should().BeEmpty();
        result.SessionsError.Should().BeNull();
    }

    [Fact]
    public async Task MenteeDashboard_AllSectionsFail_ReturnsAllErrors()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockMenteeProvider.GetActiveSessionsAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Sessions failed"));
        _mockMenteeProvider.GetRecommendedMentorsAsync(userId, 3, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Recommendations failed"));
        _mockMenteeProvider.GetProgressStatsAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Stats failed"));
        _mockMenteeProvider.GetSummaryBarAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Summary failed"));

        var handler = new GetMenteeDashboardHandler(_mockMenteeProvider, _menteeLogger);

        // Act
        var result = await handler.Handle(new GetMenteeDashboardQuery(userId), CancellationToken.None);

        // Assert — all sections have errors with defaults
        result.SessionsError.Should().NotBeNull();
        result.RecommendationsError.Should().NotBeNull();
        result.StatsError.Should().NotBeNull();
        result.ActiveSessions.Should().BeEmpty();
        result.RecommendedMentors.Should().BeEmpty();
        result.Stats.CompletedSessionsCount.Should().Be(0);
    }

    [Fact]
    public async Task MenteeDashboard_Top3Mentors_RequestsLimitOf3()
    {
        // Arrange (Req 10.1 — top 3 recommended mentors)
        var userId = Guid.NewGuid();
        _mockMenteeProvider.GetActiveSessionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActiveSessionCardDto>());
        _mockMenteeProvider.GetRecommendedMentorsAsync(userId, 3, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RecommendedMentorDto>());
        _mockMenteeProvider.GetProgressStatsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MenteeProgressStatsDto(0, 0, 0, 0));
        _mockMenteeProvider.GetSummaryBarAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new MenteeSummaryBarDto(0, 0, 0));

        var handler = new GetMenteeDashboardHandler(_mockMenteeProvider, _menteeLogger);

        // Act
        await handler.Handle(new GetMenteeDashboardQuery(userId), CancellationToken.None);

        // Assert — verifies limit=3 is passed to the provider
        await _mockMenteeProvider.Received(1).GetRecommendedMentorsAsync(userId, 3, Arg.Any<CancellationToken>());
    }

    // --- GetMentorDashboardHandler Tests ---

    [Fact]
    public async Task MentorDashboard_AllSectionsLoad_ReturnsCompleteData()
    {
        // Arrange (Req 11.1, 11.6)
        var userId = Guid.NewGuid();
        var requests = new List<PendingRequestDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Emma Chen", "Career transition to cloud", 85, DateTime.UtcNow.AddDays(-3)),
            new(Guid.NewGuid(), Guid.NewGuid(), "Frank Wu", "AWS certification prep", 72, DateTime.UtcNow.AddDays(-1))
        };
        var mentees = new List<ActiveMenteeCardDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Grace Kim", "DevOps Deep Dive", "Active", 45)
        };
        var capacity = new CapacityIndicatorDto(1, 3, false);
        var availability = new AvailabilityStatusDto(true, null, null);

        _mockMentorProvider.GetPendingRequestsAsync(userId, Arg.Any<CancellationToken>()).Returns(requests);
        _mockMentorProvider.GetActiveMenteesAsync(userId, Arg.Any<CancellationToken>()).Returns(mentees);
        _mockMentorProvider.GetCapacityAsync(userId, Arg.Any<CancellationToken>()).Returns(capacity);
        _mockMentorProvider.GetAvailabilityStatusAsync(userId, Arg.Any<CancellationToken>()).Returns(availability);

        var handler = new GetMentorDashboardHandler(_mockMentorProvider, _mentorLogger);

        // Act
        var result = await handler.Handle(new GetMentorDashboardQuery(userId), CancellationToken.None);

        // Assert
        result.PendingRequests.Should().HaveCount(2);
        result.PendingRequests[0].MenteeName.Should().Be("Emma Chen");
        result.PendingRequests[0].CompatibilityScore.Should().Be(85);
        result.ActiveMentees.Should().HaveCount(1);
        result.ActiveMentees[0].MenteeName.Should().Be("Grace Kim");
        result.Capacity.ActiveMentees.Should().Be(1);
        result.Capacity.MaxMentees.Should().Be(3);
        result.Capacity.IsAtCapacity.Should().BeFalse();
        result.AvailabilityStatus.IsAvailable.Should().BeTrue();
        result.RequestsError.Should().BeNull();
        result.MenteesError.Should().BeNull();
        result.CapacityError.Should().BeNull();
    }

    [Fact]
    public async Task MentorDashboard_PendingRequestsOrderedOldestFirst()
    {
        // Arrange (Req 11.1 — ordered by request date, oldest first)
        var userId = Guid.NewGuid();
        var olderDate = DateTime.UtcNow.AddDays(-5);
        var newerDate = DateTime.UtcNow.AddDays(-1);
        var requests = new List<PendingRequestDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Oldest Request", "Goal A", 60, olderDate),
            new(Guid.NewGuid(), Guid.NewGuid(), "Newer Request", "Goal B", 75, newerDate)
        };

        _mockMentorProvider.GetPendingRequestsAsync(userId, Arg.Any<CancellationToken>()).Returns(requests);
        _mockMentorProvider.GetActiveMenteesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<ActiveMenteeCardDto>());
        _mockMentorProvider.GetCapacityAsync(userId, Arg.Any<CancellationToken>()).Returns(new CapacityIndicatorDto(0, 3, false));
        _mockMentorProvider.GetAvailabilityStatusAsync(userId, Arg.Any<CancellationToken>()).Returns(new AvailabilityStatusDto(true, null, null));

        var handler = new GetMentorDashboardHandler(_mockMentorProvider, _mentorLogger);

        // Act
        var result = await handler.Handle(new GetMentorDashboardQuery(userId), CancellationToken.None);

        // Assert
        result.PendingRequests[0].RequestedAt.Should().BeBefore(result.PendingRequests[1].RequestedAt);
    }

    [Fact]
    public async Task MentorDashboard_AtCapacity_IndicatorShowsTrue()
    {
        // Arrange (Req 11.6 — capacity indicator)
        var userId = Guid.NewGuid();
        _mockMentorProvider.GetPendingRequestsAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<PendingRequestDto>());
        _mockMentorProvider.GetActiveMenteesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<ActiveMenteeCardDto>());
        _mockMentorProvider.GetCapacityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new CapacityIndicatorDto(3, 3, true));
        _mockMentorProvider.GetAvailabilityStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AvailabilityStatusDto(true, null, null));

        var handler = new GetMentorDashboardHandler(_mockMentorProvider, _mentorLogger);

        // Act
        var result = await handler.Handle(new GetMentorDashboardQuery(userId), CancellationToken.None);

        // Assert
        result.Capacity.IsAtCapacity.Should().BeTrue();
        result.Capacity.ActiveMentees.Should().Be(3);
        result.Capacity.MaxMentees.Should().Be(3);
    }

    [Fact]
    public async Task MentorDashboard_Unavailable_ShowsStatus()
    {
        // Arrange — availability toggle state
        var userId = Guid.NewGuid();
        _mockMentorProvider.GetPendingRequestsAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<PendingRequestDto>());
        _mockMentorProvider.GetActiveMenteesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<ActiveMenteeCardDto>());
        _mockMentorProvider.GetCapacityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new CapacityIndicatorDto(2, 5, false));
        _mockMentorProvider.GetAvailabilityStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AvailabilityStatusDto(false, "On holiday", DateTime.UtcNow.AddDays(14)));

        var handler = new GetMentorDashboardHandler(_mockMentorProvider, _mentorLogger);

        // Act
        var result = await handler.Handle(new GetMentorDashboardQuery(userId), CancellationToken.None);

        // Assert
        result.AvailabilityStatus.IsAvailable.Should().BeFalse();
        result.AvailabilityStatus.UnavailabilityReason.Should().Be("On holiday");
        result.AvailabilityStatus.ReturnDate.Should().NotBeNull();
    }

    [Fact]
    public async Task MentorDashboard_RequestsFailOtherSectionsSucceed_PartialResult()
    {
        // Arrange — per-section error recovery (Req 11.7)
        var userId = Guid.NewGuid();
        _mockMentorProvider.GetPendingRequestsAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Sessions table unavailable"));
        _mockMentorProvider.GetActiveMenteesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<ActiveMenteeCardDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Test Mentee", "Session X", "Active", 30)
            });
        _mockMentorProvider.GetCapacityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new CapacityIndicatorDto(1, 3, false));
        _mockMentorProvider.GetAvailabilityStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AvailabilityStatusDto(true, null, null));

        var handler = new GetMentorDashboardHandler(_mockMentorProvider, _mentorLogger);

        // Act
        var result = await handler.Handle(new GetMentorDashboardQuery(userId), CancellationToken.None);

        // Assert — requests section has error, others still loaded
        result.RequestsError.Should().NotBeNull();
        result.RequestsError!.Section.Should().Be("requests");
        result.RequestsError.CanRetry.Should().BeTrue();
        result.PendingRequests.Should().BeEmpty();
        result.ActiveMentees.Should().HaveCount(1);
        result.Capacity.ActiveMentees.Should().Be(1);
        result.MenteesError.Should().BeNull();
        result.CapacityError.Should().BeNull();
    }

    [Fact]
    public async Task MentorDashboard_AllSectionsFail_ReturnsAllErrors()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockMentorProvider.GetPendingRequestsAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Requests failed"));
        _mockMentorProvider.GetActiveMenteesAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Mentees failed"));
        _mockMentorProvider.GetCapacityAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Capacity failed"));
        _mockMentorProvider.GetAvailabilityStatusAsync(userId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Availability failed"));

        var handler = new GetMentorDashboardHandler(_mockMentorProvider, _mentorLogger);

        // Act
        var result = await handler.Handle(new GetMentorDashboardQuery(userId), CancellationToken.None);

        // Assert
        result.RequestsError.Should().NotBeNull();
        result.MenteesError.Should().NotBeNull();
        result.CapacityError.Should().NotBeNull();
        result.PendingRequests.Should().BeEmpty();
        result.ActiveMentees.Should().BeEmpty();
        result.Capacity.ActiveMentees.Should().Be(0);
    }

    [Fact]
    public async Task MentorDashboard_EmptyState_NoPendingRequests()
    {
        // Arrange — empty state for mentor with no activity
        var userId = Guid.NewGuid();
        _mockMentorProvider.GetPendingRequestsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PendingRequestDto>());
        _mockMentorProvider.GetActiveMenteesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActiveMenteeCardDto>());
        _mockMentorProvider.GetCapacityAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new CapacityIndicatorDto(0, 5, false));
        _mockMentorProvider.GetAvailabilityStatusAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AvailabilityStatusDto(true, null, null));

        var handler = new GetMentorDashboardHandler(_mockMentorProvider, _mentorLogger);

        // Act
        var result = await handler.Handle(new GetMentorDashboardQuery(userId), CancellationToken.None);

        // Assert — empty lists signal empty state to frontend for call-to-action guidance
        result.PendingRequests.Should().BeEmpty();
        result.ActiveMentees.Should().BeEmpty();
        result.Capacity.ActiveMentees.Should().Be(0);
        result.RequestsError.Should().BeNull();
        result.MenteesError.Should().BeNull();
    }

    [Fact]
    public async Task MentorDashboard_PendingRequestsIncludeCompatibilityScores()
    {
        // Arrange (Req 11.1, 11.2 — requests include compatibility scores)
        var userId = Guid.NewGuid();
        var requests = new List<PendingRequestDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "High Match", "Career switch", 95, DateTime.UtcNow.AddDays(-2)),
            new(Guid.NewGuid(), Guid.NewGuid(), "Low Match", "Exploration", 42, DateTime.UtcNow.AddDays(-1))
        };

        _mockMentorProvider.GetPendingRequestsAsync(userId, Arg.Any<CancellationToken>()).Returns(requests);
        _mockMentorProvider.GetActiveMenteesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<ActiveMenteeCardDto>());
        _mockMentorProvider.GetCapacityAsync(userId, Arg.Any<CancellationToken>()).Returns(new CapacityIndicatorDto(0, 3, false));
        _mockMentorProvider.GetAvailabilityStatusAsync(userId, Arg.Any<CancellationToken>()).Returns(new AvailabilityStatusDto(true, null, null));

        var handler = new GetMentorDashboardHandler(_mockMentorProvider, _mentorLogger);

        // Act
        var result = await handler.Handle(new GetMentorDashboardQuery(userId), CancellationToken.None);

        // Assert
        result.PendingRequests.Should().AllSatisfy(r => r.CompatibilityScore.Should().BeInRange(0, 100));
        result.PendingRequests[0].CompatibilityScore.Should().Be(95);
        result.PendingRequests[1].CompatibilityScore.Should().Be(42);
    }
}
