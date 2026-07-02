using GuidedMentor.Identity.Application.Commands.Auth;
using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GuidedMentor.Identity.Tests.Commands.Auth;

public sealed class VerifyMagicLinkHandlerTests
{
    private readonly IMagicLinkService _magicLinkService = Substitute.For<IMagicLinkService>();
    private readonly ILogger<VerifyMagicLinkHandler> _logger = Substitute.For<ILogger<VerifyMagicLinkHandler>>();
    private readonly VerifyMagicLinkHandler _handler;

    public VerifyMagicLinkHandlerTests()
    {
        _handler = new VerifyMagicLinkHandler(_magicLinkService, _logger);
    }

    [Fact]
    public async Task Handle_ValidTokenAndEmail_ReturnsAuthTokens()
    {
        // Arrange
        var command = new VerifyMagicLinkCommand("user@example.com", "valid-token-guid");
        var expectedResponse = new AuthResponse("access-token", "refresh-token", "id-token", null, 900);
        _magicLinkService.VerifyAndAuthenticateAsync(command.Email, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponse>.Success(expectedResponse));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.IdToken.Should().Be("id-token");
        result.Value.ExpiresIn.Should().Be(900);
    }

    [Fact]
    public async Task Handle_EmptyToken_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyMagicLinkCommand("user@example.com", "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid or missing token.");
    }

    [Fact]
    public async Task Handle_WhitespaceToken_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyMagicLinkCommand("user@example.com", "   ");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid or missing token.");
    }

    [Fact]
    public async Task Handle_EmptyEmail_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyMagicLinkCommand("", "some-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid or missing email.");
    }

    [Fact]
    public async Task Handle_WhitespaceEmail_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyMagicLinkCommand("   ", "some-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid or missing email.");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyMagicLinkCommand("user@example.com", "expired-token");
        _magicLinkService.VerifyAndAuthenticateAsync(command.Email, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponse>.Failure("Token has expired."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Token has expired.");
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyMagicLinkCommand("user@example.com", "used-token");
        _magicLinkService.VerifyAndAuthenticateAsync(command.Email, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponse>.Failure("Token has already been used."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Token has already been used.");
    }

    [Fact]
    public async Task Handle_EmailMismatch_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyMagicLinkCommand("wrong@example.com", "valid-token");
        _magicLinkService.VerifyAndAuthenticateAsync(command.Email, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponse>.Failure("Token verification failed."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Token verification failed.");
    }

    [Fact]
    public async Task Handle_NonExistentToken_ReturnsFailure()
    {
        // Arrange
        var command = new VerifyMagicLinkCommand("user@example.com", "non-existent-token");
        _magicLinkService.VerifyAndAuthenticateAsync(command.Email, command.Token, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResponse>.Failure("Token not found."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Token not found.");
    }
}
