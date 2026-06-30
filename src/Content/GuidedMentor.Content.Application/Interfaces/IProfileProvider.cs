using GuidedMentor.Content.Application.Plugins.Dtos;

namespace GuidedMentor.Content.Application.Interfaces;

/// <summary>
/// Provides profile data for session plan generation.
/// Implemented by Infrastructure to cross the bounded context boundary
/// (e.g., via DynamoDB read or internal API call to Identity context).
/// </summary>
public interface IProfileProvider
{
    /// <summary>
    /// Retrieves the mentee profile needed for AI session plan generation.
    /// </summary>
    Task<MenteeProfileDto?> GetMenteeProfileAsync(Guid menteeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the mentor profile needed for AI session plan generation.
    /// </summary>
    Task<MentorProfileDto?> GetMentorProfileAsync(Guid mentorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves session participant IDs (mentee and mentor) for a given session.
    /// </summary>
    Task<(Guid MenteeId, Guid MentorId)?> GetSessionParticipantsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
