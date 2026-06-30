namespace GuidedMentor.Content.Application.Interfaces;

/// <summary>
/// Publishes events from the Content bounded context to Amazon EventBridge.
/// Used for scheduling delayed retries and cross-context event communication.
/// </summary>
public interface IContentEventPublisher
{
    /// <summary>
    /// Publishes a PlanGenerationFailed event to EventBridge with a 5-minute delay,
    /// enabling asynchronous retry of session plan generation.
    /// </summary>
    /// <param name="sessionId">The session whose plan generation failed.</param>
    /// <param name="menteeId">The mentee in the session.</param>
    /// <param name="mentorId">The mentor in the session.</param>
    /// <param name="reason">A description of why generation failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishPlanGenerationFailedAsync(
        Guid sessionId,
        Guid menteeId,
        Guid mentorId,
        string reason,
        CancellationToken cancellationToken = default);
}
