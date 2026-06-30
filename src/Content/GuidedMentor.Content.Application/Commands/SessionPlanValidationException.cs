namespace GuidedMentor.Content.Application.Commands;

/// <summary>
/// Thrown when AI-generated session plan output fails validation (schema, PII, or harmful content).
/// This exception triggers Polly retry since the AI model may produce a valid response on next attempt.
/// </summary>
public sealed class SessionPlanValidationException : Exception
{
    public SessionPlanValidationException(string message) : base(message) { }

    public SessionPlanValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
