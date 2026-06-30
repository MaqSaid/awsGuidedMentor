namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Represents the lifecycle states of a mentorship session.
/// </summary>
public enum SessionStatus
{
    /// <summary>Waiting for the mentor to accept or decline.</summary>
    PendingAcceptance,

    /// <summary>Accepted by mentor, waiting for AI plan generation.</summary>
    PendingPlan,

    /// <summary>Plan generated, session is actively in progress.</summary>
    Active,

    /// <summary>Mentee has marked complete, waiting for mentor confirmation.</summary>
    MenteeCompleted,

    /// <summary>Both parties confirmed — session fully completed.</summary>
    Completed,

    /// <summary>Escalated after 14 days without mutual confirmation.</summary>
    Unresolved
}
