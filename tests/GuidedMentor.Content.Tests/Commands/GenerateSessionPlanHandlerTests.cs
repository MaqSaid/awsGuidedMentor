using GuidedMentor.Content.Application.Commands;
using GuidedMentor.Content.Application.Interfaces;
using GuidedMentor.Content.Application.Plugins;
using GuidedMentor.Content.Application.Plugins.Dtos;
using GuidedMentor.Content.Application.Services;
using GuidedMentor.Content.Domain;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly;
using Polly.Registry;

namespace GuidedMentor.Content.Tests.Commands;

/// <summary>
/// Unit tests for GenerateSessionPlanHandler — the MediatR command handler that orchestrates
/// session plan generation with Polly resilience, output validation, persistence, and notifications.
/// 
/// Validates: Requirements 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 24.5
/// </summary>
public sealed class GenerateSessionPlanHandlerTests
{
    private readonly IChatClient _chatClient;
    private readonly ISessionPlanRepository _sessionPlanRepository;
    private readonly IContentEventPublisher _eventPublisher;
    private readonly IContentNotificationPublisher _notificationPublisher;
    private readonly IBedrockMetricsPublisher _metricsPublisher;
    private readonly SessionPlanPlugin _plugin;
    private readonly OutputValidator _outputValidator;
    private readonly GenerateSessionPlanHandler _sut;

    private static readonly MenteeProfileDto TestMenteeProfile = new(
        DisplayName: "Jane Doe",
        Chapter: "Sydney",
        Skills: ["Lambda", "DynamoDB", "CloudFormation"],
        YearsOfExperience: 3,
        ExperienceLevel: "intermediate",
        PrimaryGoal: "skill_development",
        GoalDescription: "I want to learn serverless architecture patterns and best practices",
        PreferredDuration: "8_weeks");

    private static readonly MentorProfileDto TestMentorProfile = new(
        DisplayName: "John Smith",
        Chapter: "Sydney",
        ExpertiseAreas: ["Lambda", "DynamoDB", "Step Functions", "CDK"],
        Topics: ["Serverless Architecture", "Cost Optimization", "Security"],
        YearsOfExperience: 10,
        ProfessionalTitle: "Principal Engineer",
        CompanyName: "AWS");

    private static readonly string ValidPlanJson = """
        {
            "sessionTitle": "Serverless Architecture Deep Dive",
            "agenda": [
                {"title": "Introduction", "durationMinutes": 5, "description": "Welcome and goal setting for the session"},
                {"title": "Lambda Best Practices", "durationMinutes": 10, "description": "Discuss cold starts, memory tuning, and timeouts"},
                {"title": "Hands-on: Step Functions", "durationMinutes": 10, "description": "Build a simple workflow together"},
                {"title": "Architecture Review", "durationMinutes": 5, "description": "Review mentee current architecture"},
                {"title": "Action Items", "durationMinutes": 5, "description": "Define next steps and follow-up tasks"}
            ],
            "preworkTasks": [
                "Review AWS Lambda documentation on cold starts",
                "Set up a test AWS account with Lambda configured"
            ],
            "followUpTasks": [
                "Implement the Step Functions workflow discussed",
                "Write unit tests for Lambda handlers"
            ]
        }
        """;

    public GenerateSessionPlanHandlerTests()
    {
        _chatClient = Substitute.For<IChatClient>();
        _sessionPlanRepository = Substitute.For<ISessionPlanRepository>();
        _eventPublisher = Substitute.For<IContentEventPublisher>();
        _notificationPublisher = Substitute.For<IContentNotificationPublisher>();
        _metricsPublisher = Substitute.For<IBedrockMetricsPublisher>();

        _plugin = new SessionPlanPlugin(
            _chatClient,
            NullLogger<SessionPlanPlugin>.Instance);

        _outputValidator = new OutputValidator();

        // Use a no-op resilience pipeline for unit tests (no actual retry delays)
        var pipelineProvider = Substitute.For<ResiliencePipelineProvider<string>>();
        pipelineProvider.GetPipeline("bedrock").Returns(ResiliencePipeline.Empty);

        _sut = new GenerateSessionPlanHandler(
            _plugin,
            _outputValidator,
            _sessionPlanRepository,
            _eventPublisher,
            _notificationPublisher,
            _metricsPublisher,
            pipelineProvider,
            NullLogger<GenerateSessionPlanHandler>.Instance);
    }

    private GenerateSessionPlanCommand CreateCommand(Guid? sessionId = null) =>
        new(
            SessionId: sessionId ?? Guid.NewGuid(),
            MenteeId: Guid.NewGuid(),
            MentorId: Guid.NewGuid(),
            MenteeProfile: TestMenteeProfile,
            MentorProfile: TestMentorProfile);

    private void SetupChatClientToReturn(string responseText)
    {
        var chatMessage = new ChatMessage(ChatRole.Assistant, responseText);
        var chatResponse = new ChatResponse(chatMessage)
        {
            ModelId = "anthropic.claude-sonnet-4-20250514-v1:0",
            Usage = new UsageDetails
            {
                InputTokenCount = 1200,
                OutputTokenCount = 800
            }
        };

        _chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(chatResponse);
    }

    // ── Success Path Tests ──

    [Fact]
    public async Task Handle_ValidPlanGenerated_PersistsPlanAndNotifiesBothParties()
    {
        // Arrange
        var command = CreateCommand();
        SetupChatClientToReturn(ValidPlanJson);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.SessionTitle.Should().Be("Serverless Architecture Deep Dive");
        result.Value.Agenda.Should().HaveCount(5);
        result.Value.Agenda.Sum(a => a.DurationMinutes).Should().Be(35);

        // Verify persistence (includes model version tracking)
        await _sessionPlanRepository.Received(1).SavePlanAsync(
            command.SessionId,
            Arg.Is<SessionPlan>(p => p.SessionTitle == "Serverless Architecture Deep Dive"),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Verify notification to both parties
        await _notificationPublisher.Received(1).NotifySessionPlanReadyAsync(
            command.MenteeId,
            command.MentorId,
            command.SessionId,
            "Serverless Architecture Deep Dive",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidPlanGenerated_PublishesTokenMetrics()
    {
        // Arrange
        var command = CreateCommand();
        SetupChatClientToReturn(ValidPlanJson);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _metricsPublisher.Received(1).PublishTokenUsageAsync(
            1200, 800, command.SessionId, Arg.Any<CancellationToken>());
    }

    // ── Failure Path Tests ──

    [Fact]
    public async Task Handle_PluginReturnsNull_SetsPendingPlanAndPublishesFailedEvent()
    {
        // Arrange — chat client returns empty response which makes plugin return null
        var command = CreateCommand();
        var chatMessage = new ChatMessage(ChatRole.Assistant, "");
        var chatResponse = new ChatResponse(chatMessage);

        _chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(chatResponse);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("failed");

        await _sessionPlanRepository.Received(1).SetPendingPlanStatusAsync(
            command.SessionId, Arg.Any<CancellationToken>());

        await _eventPublisher.Received(1).PublishPlanGenerationFailedAsync(
            command.SessionId,
            command.MenteeId,
            command.MentorId,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        await _notificationPublisher.Received(1).NotifyPlanGenerationDelayedAsync(
            command.MenteeId,
            command.MentorId,
            command.SessionId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ChatClientThrows_SetsPendingPlanAndPublishesFailedEvent()
    {
        // Arrange — simulate Bedrock API failure (after Polly exhausts retries, it rethrows)
        var command = CreateCommand();
        _chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Bedrock service unavailable"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        await _sessionPlanRepository.Received(1).SetPendingPlanStatusAsync(
            command.SessionId, Arg.Any<CancellationToken>());

        await _eventPublisher.Received(1).PublishPlanGenerationFailedAsync(
            command.SessionId,
            command.MenteeId,
            command.MentorId,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OutputValidationFails_ThrowsSessionPlanValidationException()
    {
        // Arrange — plan with PII that will fail output validation
        var planWithPii = """
            {
                "sessionTitle": "Contact user@test.com for mentoring",
                "agenda": [
                    {"title": "Intro", "durationMinutes": 5, "description": "Welcome"},
                    {"title": "Deep Dive", "durationMinutes": 15, "description": "Technical discussion"},
                    {"title": "Practice", "durationMinutes": 10, "description": "Hands-on work"},
                    {"title": "Wrap-up", "durationMinutes": 5, "description": "Summary"}
                ],
                "preworkTasks": ["Review docs", "Setup environment"],
                "followUpTasks": ["Complete exercise", "Write summary"]
            }
            """;

        var command = CreateCommand();
        SetupChatClientToReturn(planWithPii);

        // Act — the validation exception propagates as an unhandled exception
        // which the handler catches and treats as a generation failure
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — since the Polly pipeline is empty (no retry), the validation exception
        // propagates to the handler's outer catch block, triggering failure handling
        result.IsFailure.Should().BeTrue();

        await _sessionPlanRepository.Received(1).SetPendingPlanStatusAsync(
            command.SessionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_ThrowsOperationCancelledException()
    {
        // Arrange
        var command = CreateCommand();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.Handle(command, cts.Token));
    }

    // ── Metrics Failure Graceful Degradation ──

    [Fact]
    public async Task Handle_MetricsPublishingFails_DoesNotFailPipeline()
    {
        // Arrange
        var command = CreateCommand();
        SetupChatClientToReturn(ValidPlanJson);

        _metricsPublisher
            .PublishTokenUsageAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("CloudWatch timeout"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — plan generation should still succeed despite metrics failure
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionTitle.Should().Be("Serverless Architecture Deep Dive");
    }

    // ── Notification on Success ──

    [Fact]
    public async Task Handle_Success_DoesNotPublishFailureEvent()
    {
        // Arrange
        var command = CreateCommand();
        SetupChatClientToReturn(ValidPlanJson);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — no failure events should be published
        await _eventPublisher.DidNotReceive().PublishPlanGenerationFailedAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _notificationPublisher.DidNotReceive().NotifyPlanGenerationDelayedAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        await _sessionPlanRepository.DidNotReceive().SetPendingPlanStatusAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
