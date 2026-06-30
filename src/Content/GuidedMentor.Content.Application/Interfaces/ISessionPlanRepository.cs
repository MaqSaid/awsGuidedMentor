using GuidedMentor.Content.Domain;

namespace GuidedMentor.Content.Application.Interfaces;

/// <summary>
/// Repository interface for persisting session plans to the Sessions_Table in DynamoDB.
/// Decouples the Content bounded context from the infrastructure layer.
/// </summary>
public interface ISessionPlanRepository
{
    /// <summary>
    /// Persists a generated session plan to the Sessions_Table and sets status to Active.
    /// Records the Bedrock model version used for generation (ISO 42001 compliance).
    /// </summary>
    /// <param name="sessionId">The session to attach the plan to.</param>
    /// <param name="plan">The validated session plan to persist.</param>
    /// <param name="bedrockModelVersion">The Bedrock model version that generated this plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SavePlanAsync(Guid sessionId, SessionPlan plan, string bedrockModelVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the session status to pending_plan when all generation attempts fail.
    /// This signals that asynchronous retry via EventBridge is pending.
    /// </summary>
    /// <param name="sessionId">The session to mark as pending plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetPendingPlanStatusAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the admin review status of a session plan.
    /// Implements human oversight per ISO 42001 requirement 8.4.
    /// </summary>
    /// <param name="sessionId">The session whose plan is being reviewed.</param>
    /// <param name="reviewedByAdminId">The admin performing the review.</param>
    /// <param name="reviewStatus">The review outcome (approved or flagged).</param>
    /// <param name="flagReason">Optional reason when flagging (required if status is flagged).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateReviewStatusAsync(
        Guid sessionId,
        Guid reviewedByAdminId,
        string reviewStatus,
        string? flagReason,
        CancellationToken cancellationToken = default);
}
