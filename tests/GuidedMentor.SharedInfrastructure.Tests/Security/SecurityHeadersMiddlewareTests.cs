using FluentAssertions;
using GuidedMentor.SharedInfrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GuidedMentor.SharedInfrastructure.Tests.Security;

/// <summary>
/// Tests for SecurityHeadersMiddleware ensuring all required security headers
/// are set on every response: CSP, X-Content-Type-Options, X-Frame-Options, HSTS.
/// </summary>
public sealed class SecurityHeadersMiddlewareTests
{
    private readonly SecurityOptions _options = new()
    {
        HstsMaxAgeSeconds = 31_536_000,
        ContentSecurityPolicy = "default-src 'self'; frame-ancestors 'none';"
    };

    private SecurityHeadersMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new SecurityHeadersMiddleware(next, Options.Create(_options));
    }

    [Fact]
    public async Task Should_Set_XContentTypeOptions_Nosniff()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
    }

    [Fact]
    public async Task Should_Set_XFrameOptions_Deny()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
    }

    [Fact]
    public async Task Should_Set_StrictTransportSecurity_With_MaxAge()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["Strict-Transport-Security"].ToString()
            .Should().Contain("max-age=31536000")
            .And.Contain("includeSubDomains")
            .And.Contain("preload");
    }

    [Fact]
    public async Task Should_Set_ContentSecurityPolicy()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["Content-Security-Policy"].ToString()
            .Should().Be(_options.ContentSecurityPolicy);
    }

    [Fact]
    public async Task Should_Set_ReferrerPolicy()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["Referrer-Policy"].ToString()
            .Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task Should_Set_PermissionsPolicy()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["Permissions-Policy"].ToString()
            .Should().Contain("camera=()");
    }

    [Fact]
    public async Task Should_Disable_XssProtection()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("0");
    }

    [Fact]
    public async Task Should_Call_Next_Middleware()
    {
        var nextCalled = false;
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
