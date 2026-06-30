using GuidedMentor.Mentoring.Domain.Entities;

namespace GuidedMentor.Mentoring.Domain.Repositories;

/// <summary>
/// Repository interface for Session persistence in DynamoDB.
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Creates a new session record in the Sessions_Table with PendingAcceptance status.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="menteeId">The mentee in the session.</param>
    /// <param name="mentorId">The mentor in the session.</param>
    /// <param name="lockId">The associated lock identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreatePendingSessionAsync(
        SessionId sessionId,
        MenteeId menteeId,
        MentorId mentorId,
        LockId lockId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a session by its identifier.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session, or null if not found.</returns>
    Task<Session?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current state of a session to the Sessions_Table.
    /// </summary>
    /// <param name="session">The session to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session record (used when a request is declined).
    /// </summary>
    /// <param name="sessionId">The session to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(SessionId sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions in MenteeCompleted status where the mentee completed
    /// at least the specified number of days ago (for reminder/escalation scheduling).
    /// </summary>
    /// <param name="menteeCompletedDaysAgo">Minimum days since mentee completion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sessions awaiting mentor confirmation beyond the threshold.</returns>
    Task<IReadOnlyList<Session>> GetSessionsAwaitingConfirmationAsync(
        int menteeCompletedDaysAgo,
        CancellationToken cancellationToken = default);
}
