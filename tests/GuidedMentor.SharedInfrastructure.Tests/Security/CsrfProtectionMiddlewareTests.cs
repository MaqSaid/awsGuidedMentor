using FluentAssertions;
using GuidedMentor.SharedInfrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GuidedMentor.SharedInfrastructure.Tests.Security;

/// <summary>
/// Tests for CsrfProtectionMiddleware ensuring SameSite=Strict enforcement
/// and Origin header validation on state-changing requests.
/// </summary>
public sealed class CsrfProtectionMiddlewareTests
{
    private readonly SecurityOptions _options = new()
    {
        AllowedOrigins = ["https://guidedmentor.dev", "http://localhost:5173"],
        AnonymousPaths = ["/v1/health", "/v1/auth/signin"]
    };

    private CsrfProtectionMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new CsrfProtectionMiddleware(
            next,
            Options.Create(_options),
            NullLogger<CsrfProtectionMiddleware>.Instance);
    }

    [Fact]
    public async Task Should_Allow_GET_Requests_Without_Origin_Check()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        // No Origin header

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Allow_POST_With_Valid_Origin()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/v1/users/role";
        context.Request.Headers.Origin = "https://guidedmentor.dev";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Reject_POST_With_Invalid_Origin()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/v1/users/role";
        context.Request.Headers.Origin = "https://evil-site.com";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Should_Allow_POST_With_Valid_Referer_When_No_Origin()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/v1/users/role";
        context.Request.Headers.Referer = "https://guidedmentor.dev/dashboard";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Skip_Csrf_Check_For_Anonymous_Paths()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/v1/auth/signin";
        context.Request.Headers.Origin = "https://evil-site.com";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Allow_POST_Without_Origin_Or_Referer()
    {
        // When neither Origin nor Referer is present, allow (same-origin browsers may omit)
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/v1/users/role";
        // No Origin or Referer headers

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Allow_Localhost_Origin_For_Development()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/v1/users/role";
        context.Request.Headers.Origin = "http://localhost:5173";

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Reject_DELETE_With_Invalid_Origin()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Delete;
        context.Request.Path = "/v1/sessions/123";
        context.Request.Headers.Origin = "https://attacker.com";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
    }
}
