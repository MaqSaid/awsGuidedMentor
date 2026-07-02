using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Domain event raised when a new opportunity posting is published.
/// Triggers notifications to matched mentees and skill-matched opt-in mentees.
/// </summary>
public sealed record OpportunityPublishedEvent(
    OpportunityPostingId PostingId,
    MentorId MentorId,
    OpportunityType Type,
    IReadOnlyList<string> RequiredSkills,
    DateTime OccurredAt) : IDomainEvent;
