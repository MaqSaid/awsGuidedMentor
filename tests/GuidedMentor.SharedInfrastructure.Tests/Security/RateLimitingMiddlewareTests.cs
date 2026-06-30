using FluentAssertions;
using GuidedMentor.SharedInfrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GuidedMentor.SharedInfrastructure.Tests.Security;

/// <summary>
/// Tests for RateLimitingMiddleware ensuring per-user sliding window rate limiting
/// at 100 requests/minute with 429 + Retry-After header.
/// </summary>
public sealed class RateLimitingMiddlewareTests
{
    private readonly SecurityOptions _options = new()
    {
        RateLimitMaxRequests = 5,  // Reduced for testing
        RateLimitWindowSeconds = 60
    };

    private RateLimitingMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new RateLimitingMiddleware(
            next,
            Options.Create(_options),
            NullLogger<RateLimitingMiddleware>.Instance);
    }

    [Fact]
    public async Task Should_Allow_Requests_Under_Limit()
    {
        var callCount = 0;
        var middleware = CreateMiddleware(_ =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        for (var i = 0; i < 5; i++)
        {
            var context = CreateAuthenticatedContext("user-1");
            await middleware.InvokeAsync(context);
            context.Response.StatusCode.Should().NotBe(429);
        }

        callCount.Should().Be(5);
    }

    [Fact]
    public async Task Should_Return_429_When_Limit_Exceeded()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Exhaust the limit
        for (var i = 0; i < 5; i++)
        {
            var ctx = CreateAuthenticatedContext("user-2");
            await middleware.InvokeAsync(ctx);
        }

        // Next request should be rate limited
        var context = CreateAuthenticatedContext("user-2");
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task Should_Include_RetryAfter_Header_When_Limited()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        for (var i = 0; i < 5; i++)
        {
            var ctx = CreateAuthenticatedContext("user-3");
            await middleware.InvokeAsync(ctx);
        }

        var context = CreateAuthenticatedContext("user-3");
        await middleware.InvokeAsync(context);

        context.Response.Headers["Retry-After"].ToString().Should().NotBeNullOrEmpty();
        int.Parse(context.Response.Headers["Retry-After"].ToString()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Rate_Limit_Independently_Per_User()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Exhaust limit for user-4
        for (var i = 0; i < 5; i++)
        {
            var ctx = CreateAuthenticatedContext("user-4");
            await middleware.InvokeAsync(ctx);
        }

        // user-5 should still be allowed
        var context = CreateAuthenticatedContext("user-5");
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task Should_Skip_Rate_Limiting_For_Unauthenticated_Requests()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // No UserId in Items (unauthenticated)
        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task Should_Add_RateLimit_Headers_To_Response()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        var context = CreateAuthenticatedContext("user-6");
        await middleware.InvokeAsync(context);

        context.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("5");
        context.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("4");
        context.Response.Headers["X-RateLimit-Reset"].ToString().Should().NotBeNullOrEmpty();
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string userId)
    {
        var context = new DefaultHttpContext();
        context.Items["UserId"] = userId;
        return context;
    }
}
