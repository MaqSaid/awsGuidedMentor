namespace GuidedMentor.SharedInfrastructure.ErrorHandling.Exceptions;

/// <summary>
/// Thrown when a resource conflict occurs (e.g., mentor lock conflicts, concurrent writes).
/// Maps to HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException()
        : base("A conflict occurred with the current state of the resource.")
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
