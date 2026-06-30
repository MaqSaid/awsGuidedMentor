using FluentAssertions;
using GuidedMentor.SharedInfrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace GuidedMentor.SharedInfrastructure.Tests.Security;

/// <summary>
/// Tests for ResourceAccessControlMiddleware ensuring users can only access their own resources.
/// JWT userId must match resource owner, else 403.
/// </summary>
public sealed class ResourceAccessControlMiddlewareTests
{
    private ResourceAccessControlMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new ResourceAccessControlMiddleware(
            next,
            NullLogger<ResourceAccessControlMiddleware>.Instance);
    }

    [Fact]
    public async Task Should_Allow_Request_When_UserId_Matches_Route()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = CreateContextWithRouteUserId("user-123", "user-123");
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_403_When_UserId_Does_Not_Match_Route()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = CreateContextWithRouteUserId("user-123", "user-456");
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Should_Allow_Request_When_No_UserId_In_Route()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Items["UserId"] = "user-123";
        // Set up authentication
        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim("sub", "user-123")],
            "Bearer");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Skip_When_User_Not_Authenticated()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        // No authentication

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    private static DefaultHttpContext CreateContextWithRouteUserId(string authenticatedUserId, string routeUserId)
    {
        var context = new DefaultHttpContext();
        context.Items["UserId"] = authenticatedUserId;

        // Set up route values
        context.Request.RouteValues = new RouteValueDictionary
        {
            ["userId"] = routeUserId
        };

        // Set up authenticated identity
        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim("sub", authenticatedUserId)],
            "Bearer");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        return context;
    }
}
