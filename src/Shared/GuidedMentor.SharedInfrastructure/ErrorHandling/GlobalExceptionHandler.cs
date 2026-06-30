using System.Net;
using FluentValidation;
using GuidedMentor.SharedInfrastructure.ErrorHandling.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.SharedInfrastructure.ErrorHandling;

/// <summary>
/// Global exception handler implementing the .NET 8+ IExceptionHandler pattern.
/// Maps known exception types to structured HTTP error responses.
/// Never exposes stack traces or internal details to the client.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items.TryGetValue("CorrelationId", out var id)
            ? id?.ToString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

        var (statusCode, error, message, fieldErrors) = MapException(exception);

        _logger.LogError(
            exception,
            "Unhandled exception [{Error}] CorrelationId={CorrelationId} Path={Path} StatusCode={StatusCode}",
            error,
            correlationId,
            httpContext.Request.Path.Value,
            statusCode);

        var response = new ApiErrorResponse
        {
            StatusCode = statusCode,
            Error = error,
            Message = message,
            CorrelationId = correlationId,
            FieldErrors = fieldErrors
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Error, string Message, Dictionary<string, string>? FieldErrors) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationEx => (
                (int)HttpStatusCode.BadRequest,
                "ValidationError",
                "One or more validation errors occurred.",
                validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => string.Join("; ", g.Select(e => e.ErrorMessage)))
            ),

            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.",
                null
            ),

            ForbiddenException => (
                (int)HttpStatusCode.Forbidden,
                "Forbidden",
                exception.Message,
                null
            ),

            NotFoundException => (
                (int)HttpStatusCode.NotFound,
                "NotFound",
                exception.Message,
                null
            ),

            ConflictException => (
                (int)HttpStatusCode.Conflict,
                "Conflict",
                exception.Message,
                null
            ),

            RateLimitedException rateLimitedEx => (
                (int)HttpStatusCode.TooManyRequests,
                "RateLimited",
                rateLimitedEx.Message,
                rateLimitedEx.RetryAfterSeconds.HasValue
                    ? new Dictionary<string, string> { ["retryAfterSeconds"] = rateLimitedEx.RetryAfterSeconds.Value.ToString() }
                    : null
            ),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                "InternalServerError",
                "An unexpected error occurred. Please try again later.",
                null
            )
        };
    }
}
