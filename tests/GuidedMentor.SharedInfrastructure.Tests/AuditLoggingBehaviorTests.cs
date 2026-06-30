using FluentAssertions;
using GuidedMentor.Observability.Logging;
using GuidedMentor.SharedInfrastructure.AuditLogging;
using GuidedMentor.SharedInfrastructure.Behaviors;
using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GuidedMentor.SharedInfrastructure.Tests;

public class AuditLoggingBehaviorTests
{
    private readonly IAuditLogWriter _auditLogWriter = Substitute.For<IAuditLogWriter>();
    private readonly ILogger<AuditLoggingBehavior<TestAuditableCommand, Result>> _logger =
        Substitute.For<ILogger<AuditLoggingBehavior<TestAuditableCommand, Result>>>();
    private readonly ILogger<AuditLoggingBehavior<TestAdminCommand, Result>> _adminLogger =
        Substitute.For<ILogger<AuditLoggingBehavior<TestAdminCommand, Result>>>();
    private readonly ILogger<AuditLoggingBehavior<TestNonAuditableCommand, Result>> _nonAuditLogger =
        Substitute.For<ILogger<AuditLoggingBehavior<TestNonAuditableCommand, Result>>>();

    [Fact]
    public async Task Handle_AuditableCommand_WritesAuditLog()
    {
        // Arrange
        var behavior = new AuditLoggingBehavior<TestAuditableCommand, Result>(
            _auditLogWriter, _logger);
        var command = new TestAuditableCommand(Guid.NewGuid(), "Resource:123");
        CorrelationContext.CurrentCorrelationId = "test-correlation-id";

        // Act
        var result = await behavior.Handle(
            command,
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _auditLogWriter.Received(1).WriteAsync(
            Arg.Is<AuditLogRecord>(r =>
                r.UserId == command.UserId.ToString() &&
                r.Action == nameof(TestAuditableCommand) &&
                r.Resource == "Resource:123" &&
                r.CorrelationId == "test-correlation-id" &&
                r.Success == true &&
                r.AdminId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuditableCommand_Failure_RecordsSuccessFalse()
    {
        // Arrange
        var behavior = new AuditLoggingBehavior<TestAuditableCommand, Result>(
            _auditLogWriter, _logger);
        var command = new TestAuditableCommand(Guid.NewGuid(), "Resource:456");
        CorrelationContext.CurrentCorrelationId = "fail-correlation";

        // Act
        var result = await behavior.Handle(
            command,
            () => Task.FromResult(Result.Failure("Something went wrong")),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        await _auditLogWriter.Received(1).WriteAsync(
            Arg.Is<AuditLogRecord>(r =>
                r.Success == false &&
                r.Action == nameof(TestAuditableCommand)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuditableCommand_Exception_RecordsSuccessFalseAndRethrows()
    {
        // Arrange
        var behavior = new AuditLoggingBehavior<TestAuditableCommand, Result>(
            _auditLogWriter, _logger);
        var command = new TestAuditableCommand(Guid.NewGuid(), "Resource:789");
        CorrelationContext.CurrentCorrelationId = "exception-correlation";

        // Act & Assert
        var act = async () => await behavior.Handle(
            command,
            () => throw new InvalidOperationException("Handler exploded"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _auditLogWriter.Received(1).WriteAsync(
            Arg.Is<AuditLogRecord>(r =>
                r.Success == false &&
                r.Action == nameof(TestAuditableCommand)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonAuditableCommand_SkipsAuditLog()
    {
        // Arrange
        var behavior = new AuditLoggingBehavior<TestNonAuditableCommand, Result>(
            _auditLogWriter, _nonAuditLogger);
        var command = new TestNonAuditableCommand();

        // Act
        var result = await behavior.Handle(
            command,
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _auditLogWriter.DidNotReceive().WriteAsync(
            Arg.Any<AuditLogRecord>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminCommand_IncludesAdminFields()
    {
        // Arrange
        var behavior = new AuditLoggingBehavior<TestAdminCommand, Result>(
            _auditLogWriter, _adminLogger);
        var adminId = Guid.NewGuid();
        var command = new TestAdminCommand(adminId, "User:target-user", "Policy violation");
        CorrelationContext.CurrentCorrelationId = "admin-correlation";

        // Act
        var result = await behavior.Handle(
            command,
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _auditLogWriter.Received(1).WriteAsync(
            Arg.Is<AuditLogRecord>(r =>
                r.AdminId == adminId.ToString() &&
                r.AdminTarget == "User:target-user" &&
                r.AdminReason == "Policy violation" &&
                r.Success == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuditWriterFails_DoesNotCauseCommandFailure()
    {
        // Arrange
        _auditLogWriter.WriteAsync(Arg.Any<AuditLogRecord>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("CloudWatch unavailable")));

        var behavior = new AuditLoggingBehavior<TestAuditableCommand, Result>(
            _auditLogWriter, _logger);
        var command = new TestAuditableCommand(Guid.NewGuid(), "Resource:resilient");
        CorrelationContext.CurrentCorrelationId = "resilient-correlation";

        // Act
        var result = await behavior.Handle(
            command,
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert - command should still succeed even if audit writer fails
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoCorrelationId_GeneratesOne()
    {
        // Arrange
        var behavior = new AuditLoggingBehavior<TestAuditableCommand, Result>(
            _auditLogWriter, _logger);
        var command = new TestAuditableCommand(Guid.NewGuid(), "Resource:no-corr");
        CorrelationContext.CurrentCorrelationId = null;

        // Act
        await behavior.Handle(
            command,
            () => Task.FromResult(Result.Success()),
            CancellationToken.None);

        // Assert
        await _auditLogWriter.Received(1).WriteAsync(
            Arg.Is<AuditLogRecord>(r =>
                !string.IsNullOrEmpty(r.CorrelationId)),
            Arg.Any<CancellationToken>());
    }
}

// Test doubles

public sealed record TestAuditableCommand(Guid UserId, string ResourceId) : IRequest<Result>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => UserId;
    string IAuditableCommand.AuditResourceId => ResourceId;
}

public sealed record TestAdminCommand(Guid AdminId, string Target, string Reason) : IRequest<Result>, IAdminCommand
{
    Guid IAuditableCommand.UserId => AdminId;
    string IAuditableCommand.AuditResourceId => Target;
    Guid IAdminCommand.AdminId => AdminId;
    string IAdminCommand.AuditTarget => Target;
    string IAdminCommand.AuditReason => Reason;
}

public sealed record TestNonAuditableCommand() : IRequest<Result>;
