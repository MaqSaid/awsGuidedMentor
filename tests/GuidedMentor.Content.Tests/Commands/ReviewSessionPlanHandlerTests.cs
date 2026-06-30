using GuidedMentor.Content.Application.Commands;
using GuidedMentor.Content.Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GuidedMentor.Content.Tests.Commands;

/// <summary>
/// Unit tests for ReviewSessionPlanHandler — admin review/flag of AI-generated session plans.
/// Implements human oversight capability per ISO 42001 requirement 8.4.
/// 
/// Validates: Requirement 21.17 (ISO 42001 — 8.4 Human oversight)
/// </summary>
public sealed class ReviewSessionPlanHandlerTests
{
    private readonly ISessionPlanRepository _sessionPlanRepository;
    private readonly ReviewSessionPlanHandler _sut;

    public ReviewSessionPlanHandlerTests()
    {
        _sessionPlanRepository = Substitute.For<ISessionPlanRepository>();
        _sut = new ReviewSessionPlanHandler(
            _sessionPlanRepository,
            NullLogger<ReviewSessionPlanHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ApproveAction_UpdatesStatusToApproved()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new ReviewSessionPlanCommand(adminId, sessionId, SessionPlanReviewAction.Approve, null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _sessionPlanRepository.Received(1).UpdateReviewStatusAsync(
            sessionId,
            adminId,
            "approved",
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FlagActionWithReason_UpdatesStatusToFlagged()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var reason = "Content appears to contain hallucinated AWS service names";
        var command = new ReviewSessionPlanCommand(adminId, sessionId, SessionPlanReviewAction.Flag, reason);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _sessionPlanRepository.Received(1).UpdateReviewStatusAsync(
            sessionId,
            adminId,
            "flagged",
            reason,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FlagActionWithoutReason_ReturnsFailure()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new ReviewSessionPlanCommand(adminId, sessionId, SessionPlanReviewAction.Flag, null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reason is required");

        await _sessionPlanRepository.DidNotReceive().UpdateReviewStatusAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FlagActionWithEmptyReason_ReturnsFailure()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new ReviewSessionPlanCommand(adminId, sessionId, SessionPlanReviewAction.Flag, "   ");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reason is required");
    }

    [Fact]
    public void Command_ImplementsIAdminCommand_WithCorrectAuditFields()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new ReviewSessionPlanCommand(adminId, sessionId, SessionPlanReviewAction.Flag, "Test reason");

        // Assert — IAdminCommand fields
        var adminCommand = (GuidedMentor.SharedKernel.IAdminCommand)command;
        adminCommand.AdminId.Should().Be(adminId);
        adminCommand.AuditTarget.Should().Be($"Session:{sessionId}");
        adminCommand.AuditReason.Should().Be("Test reason");
    }

    [Fact]
    public void Command_ApproveAction_AuditReasonIsApproved()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new ReviewSessionPlanCommand(adminId, sessionId, SessionPlanReviewAction.Approve, null);

        // Assert
        var adminCommand = (GuidedMentor.SharedKernel.IAdminCommand)command;
        adminCommand.AuditReason.Should().Be("Approved");
    }
}
