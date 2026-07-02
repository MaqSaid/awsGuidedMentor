using GuidedMentor.Identity.Application.Commands.Auth;
using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GuidedMentor.Identity.Tests.Commands.Auth;

public sealed class RequestMagicLinkHandlerTests
{
    private readonly IMagicLinkService _magicLinkService = Substitute.For<IMagicLinkService>();
    private readonly ILogger<RequestMagicLinkHandler> _logger = Substitute.For<ILogger<RequestMagicLinkHandler>>();
    private readonly RequestMagicLinkHandler _handler;

    public RequestMagicLinkHandlerTests()
    {
        _handler = new RequestMagicLinkHandler(_magicLinkService, _logger);
    }

    [Fact]
    public async Task Handle_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("user@example.com");
        _magicLinkService.CanSendAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _magicLinkService.Received(1).SendMagicLinkAsync(command.Email, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyEmail_ReturnsFailure()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Please enter a valid email address.");
        await _magicLinkService.DidNotReceive().SendMagicLinkAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailWithoutAtSymbol_ReturnsFailure()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("invalidemail.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Please enter a valid email address.");
    }

    [Fact]
    public async Task Handle_WhitespaceEmail_ReturnsFailure()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("   ");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Please enter a valid email address.");
    }

    [Fact]
    public async Task Handle_RateLimited_ReturnsFailure()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("user@example.com");
        _magicLinkService.CanSendAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Too many sign-in requests. Please wait a few minutes and try again.");
        await _magicLinkService.DidNotReceive().SendMagicLinkAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ServiceSendsEmailSuccessfully_ReturnsSuccess()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("test@guidedmentor.dev");
        _magicLinkService.CanSendAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);
        _magicLinkService.SendMagicLinkAsync(command.Email, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _magicLinkService.Received(1).SendMagicLinkAsync(command.Email, Arg.Any<CancellationToken>());
    }
}
