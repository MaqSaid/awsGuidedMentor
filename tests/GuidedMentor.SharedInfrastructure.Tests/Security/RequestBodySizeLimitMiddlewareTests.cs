using FluentAssertions;
using GuidedMentor.SharedInfrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GuidedMentor.SharedInfrastructure.Tests.Security;

/// <summary>
/// Tests for RequestBodySizeLimitMiddleware ensuring 1 MB max request body enforcement.
/// </summary>
public sealed class RequestBodySizeLimitMiddlewareTests
{
    private readonly SecurityOptions _options = new()
    {
        MaxRequestBodySizeBytes = 1_048_576  // 1 MB
    };

    private RequestBodySizeLimitMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new RequestBodySizeLimitMiddleware(
            next,
            Options.Create(_options),
            NullLogger<RequestBodySizeLimitMiddleware>.Instance);
    }

    [Fact]
    public async Task Should_Allow_Request_Under_Size_Limit()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentLength = 1024; // 1 KB

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Reject_Request_Exceeding_Size_Limit()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentLength = 2_000_000; // 2 MB (exceeds 1 MB limit)

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(413);
    }

    [Fact]
    public async Task Should_Skip_Size_Check_For_GET_Requests()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        // No content-length needed for GET

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Allow_Request_Without_ContentLength_Header()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        // No Content-Length header (chunked transfer)

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Check_PUT_Requests()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Put;
        context.Request.ContentLength = 2_000_000;

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(413);
    }

    [Fact]
    public async Task Should_Check_PATCH_Requests()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Patch;
        context.Request.ContentLength = 2_000_000;

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(413);
    }
}
