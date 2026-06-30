namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Strongly-typed identifier for the OpportunityPosting entity.
/// </summary>
public sealed record OpportunityPostingId(Guid Value)
{
    public static OpportunityPostingId New() => new(Guid.NewGuid());
}
