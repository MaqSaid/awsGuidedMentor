using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuidedMentor.SharedInfrastructure.Security;

/// <summary>
/// Middleware that enforces a maximum request body size (default 1 MB).
/// Rejects requests exceeding the limit with 413 Payload Too Large.
/// Checks Content-Length header first for early rejection, then enforces
/// the limit on the request body stream for chunked transfers.
/// </summary>
public sealed class RequestBodySizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;
    private readonly ILogger<RequestBodySizeLimitMiddleware> _logger;

    public RequestBodySizeLimitMiddleware(
        RequestDelegate next,
        IOptions<SecurityOptions> options,
        ILogger<RequestBodySizeLimitMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var maxSize = _options.MaxRequestBodySizeBytes;

        // Only check body size for methods that typically have a body
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            // Early rejection based on Content-Length header
            if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > maxSize)
            {
                _logger.LogWarning(
                    "Request body size {Size} exceeds maximum {MaxSize} bytes. Path={Path}",
                    context.Request.ContentLength.Value,
                    maxSize,
                    context.Request.Path.Value);

                await WritePayloadTooLargeResponse(context, maxSize);
                return;
            }

            // Enable request body buffering and enforce size limit for chunked transfers
            context.Request.EnableBuffering();

            var feature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
            if (feature is { IsReadOnly: false })
            {
                feature.MaxRequestBodySize = maxSize;
            }
        }

        await _next(context);
    }

    private static async Task WritePayloadTooLargeResponse(HttpContext context, long maxSize)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            statusCode = 413,
            error = "PayloadTooLarge",
            message = $"Request body exceeds the maximum allowed size of {maxSize / 1_048_576} MB.",
            correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id?.ToString() : Guid.NewGuid().ToString("D")
        });
    }
}
