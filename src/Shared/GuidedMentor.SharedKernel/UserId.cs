namespace GuidedMentor.SharedKernel;

/// <summary>
/// Strongly-typed identifier for User aggregate.
/// </summary>
public sealed record UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
}
