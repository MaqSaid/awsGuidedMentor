using GuidedMentor.Engagement.Application.DTOs;

namespace GuidedMentor.Engagement.Application.Interfaces;

/// <summary>
/// Anti-corruption layer port for retrieving mentor dashboard data from cross-context sources
/// (Sessions_Table, Mentors_Table, Mentees_Table, and compatibility score service).
/// Implemented in the Infrastructure layer with DynamoDB queries.
/// </summary>
public interface IMentorDashboardDataProvider
{
    /// <summary>
    /// Gets pending mentorship requests for the mentor, ordered by request date (oldest first).
    /// Includes mentee name, goal, and compatibility score.
    /// </summary>
    Task<IReadOnlyList<PendingRequestDto>> GetPendingRequestsAsync(
        Guid mentorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active mentee cards for the mentor (sessions in Active or MenteeCompleted status).
    /// Includes mentee name, session title, status, and progress %.
    /// </summary>
    Task<IReadOnlyList<ActiveMenteeCardDto>> GetActiveMenteesAsync(
        Guid mentorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the capacity indicator: active mentee count, max capacity, and whether at capacity.
    /// </summary>
    Task<CapacityIndicatorDto> GetCapacityAsync(
        Guid mentorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current availability status for the mentor toggle.
    /// </summary>
    Task<AvailabilityStatusDto> GetAvailabilityStatusAsync(
        Guid mentorId,
        CancellationToken cancellationToken = default);
}
