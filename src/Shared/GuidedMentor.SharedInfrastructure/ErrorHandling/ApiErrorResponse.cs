namespace GuidedMentor.SharedInfrastructure.ErrorHandling;

/// <summary>
/// Structured error response returned by all API endpoints.
/// Never exposes stack traces or internal implementation details to clients.
/// </summary>
public sealed record ApiErrorResponse
{
    public required int StatusCode { get; init; }
    public required string Error { get; init; }
    public required string Message { get; init; }
    public required string CorrelationId { get; init; }
    public Dictionary<string, string>? FieldErrors { get; init; }
}
