using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Entities;

/// <summary>
/// Aggregate root representing a mentorship session between a mentor and mentee.
/// Manages the completion flow state machine:
///   - Mentee marks complete first → sets MenteeCompletedAt, status = MenteeCompleted
///   - Mentor confirms after mentee → sets MentorCompletedAt, status = Completed
///   - Mentor attempting to complete first is rejected
///   - Mentee completion is irrevocable (cannot retract)
/// </summary>
public sealed class Session : AggregateRoot<SessionId>
{
    /// <summary>The mentee in this session.</summary>
    public MenteeId MenteeId { get; private set; }

    /// <summary>The mentor in this session.</summary>
    public MentorId MentorId { get; private set; }

    /// <summary>Current lifecycle status of the session.</summary>
    public SessionStatus Status { get; private set; }

    /// <summary>When the mentee marked this session complete (null if not yet).</summary>
    public DateTime? MenteeCompletedAt { get; private set; }

    /// <summary>When the mentor confirmed completion (null if not yet).</summary>
    public DateTime? MentorCompletedAt { get; private set; }

    /// <summary>The associated lock ID from selection.</summary>
    public LockId LockId { get; private set; }

    /// <summary>When the session was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Last update timestamp.</summary>
    public DateTime UpdatedAt { get; private set; }

    private Session()
    {
        MenteeId = null!;
        MentorId = null!;
        LockId = null!;
    }

    /// <summary>
    /// Creates a new session in PendingAcceptance status.
    /// </summary>
    public static Session CreatePending(
        SessionId sessionId,
        MenteeId menteeId,
        MentorId mentorId,
        LockId lockId)
    {
        var now = DateTime.UtcNow;
        return new Session
        {
            Id = sessionId,
            MenteeId = menteeId,
            MentorId = mentorId,
            LockId = lockId,
            Status = SessionStatus.PendingAcceptance,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Reconstitutes a session from persisted data.
    /// </summary>
    public static Session Reconstitute(
        SessionId sessionId,
        MenteeId menteeId,
        MentorId mentorId,
        LockId lockId,
        SessionStatus status,
        DateTime? menteeCompletedAt,
        DateTime? mentorCompletedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Session
        {
            Id = sessionId,
            MenteeId = menteeId,
            MentorId = mentorId,
            LockId = lockId,
            Status = status,
            MenteeCompletedAt = menteeCompletedAt,
            MentorCompletedAt = mentorCompletedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    /// <summary>
    /// Mentor accepts the pending request. Transitions to PendingPlan status.
    /// </summary>
    public Result Accept()
    {
        if (Status != SessionStatus.PendingAcceptance)
        {
            return Result.Failure("Session can only be accepted when in PendingAcceptance status.");
        }

        Status = SessionStatus.PendingPlan;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new SessionAcceptedEvent(Id, MenteeId, MentorId, UpdatedAt));
        return Result.Success();
    }

    /// <summary>
    /// Mentor declines the pending request.
    /// </summary>
    public Result Decline()
    {
        if (Status != SessionStatus.PendingAcceptance)
        {
            return Result.Failure("Session can only be declined when in PendingAcceptance status.");
        }

        Status = SessionStatus.Unresolved;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new SessionDeclinedEvent(Id, MenteeId, MentorId, UpdatedAt));
        return Result.Success();
    }

    /// <summary>
    /// Activates the session (called after plan generation succeeds).
    /// </summary>
    public Result Activate()
    {
        if (Status != SessionStatus.PendingPlan)
        {
            return Result.Failure("Session can only be activated from PendingPlan status.");
        }

        Status = SessionStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Marks the session complete by a specific role.
    /// Implements the completion state machine:
    ///   - Mentee must mark first → status becomes MenteeCompleted
    ///   - Mentor confirms after mentee → status becomes Completed
    ///   - Mentor cannot mark first → rejected
    ///   - Mentee cannot retract once marked → menteeCompletedAt is irrevocable
    /// </summary>
    /// <param name="role">The role of the user marking completion.</param>
    /// <returns>Result indicating success or failure with explanation.</returns>
    public Result MarkComplete(Role role)
    {
        return role switch
        {
            Role.Mentee => MarkMenteeComplete(),
            Role.Mentor => MarkMentorComplete(),
            _ => Result.Failure("Invalid role for completion.")
        };
    }

    private Result MarkMenteeComplete()
    {
        // Mentee cannot retract a completion mark
        if (MenteeCompletedAt.HasValue)
        {
            return Result.Failure("You have already marked this session as complete. Completion cannot be retracted.");
        }

        // Session must be in Active status for mentee to mark complete
        if (Status != SessionStatus.Active)
        {
            return Result.Failure("Session must be in Active status to mark as complete.");
        }

        MenteeCompletedAt = DateTime.UtcNow;
        Status = SessionStatus.MenteeCompleted;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    private Result MarkMentorComplete()
    {
        // Mentor cannot retract
        if (MentorCompletedAt.HasValue)
        {
            return Result.Failure("You have already confirmed completion for this session.");
        }

        // Mentor cannot mark complete before the mentee
        if (!MenteeCompletedAt.HasValue || Status != SessionStatus.MenteeCompleted)
        {
            return Result.Failure("The mentee must mark the session as complete before the mentor can confirm.");
        }

        MentorCompletedAt = DateTime.UtcNow;
        Status = SessionStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new SessionCompletedEvent(Id, MenteeId, MentorId, UpdatedAt));
        return Result.Success();
    }

    /// <summary>
    /// Escalates the session to Unresolved status (after 14-day timeout).
    /// </summary>
    public Result Escalate()
    {
        if (Status != SessionStatus.MenteeCompleted)
        {
            return Result.Failure("Only sessions in MenteeCompleted status can be escalated.");
        }

        Status = SessionStatus.Unresolved;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
