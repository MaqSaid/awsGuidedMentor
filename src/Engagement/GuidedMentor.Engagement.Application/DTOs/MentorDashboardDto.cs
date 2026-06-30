namespace GuidedMentor.Engagement.Application.DTOs;

/// <summary>
/// Rich DTO for the mentor dashboard, aggregated from multiple data sources.
/// Consumed directly by the frontend — no additional transformation needed.
/// </summary>
public sealed record MentorDashboardDto(
    IReadOnlyList<PendingRequestDto> PendingRequests,
    IReadOnlyList<ActiveMenteeCardDto> ActiveMentees,
    CapacityIndicatorDto Capacity,
    AvailabilityStatusDto AvailabilityStatus,
    DashboardSectionErrorDto? RequestsError,
    DashboardSectionErrorDto? MenteesError,
    DashboardSectionErrorDto? CapacityError);

/// <summary>
/// A pending mentorship request card shown on the mentor dashboard.
/// Ordered by request date (oldest first) with compatibility scores.
/// </summary>
public sealed record PendingRequestDto(
    Guid SessionId,
    Guid MenteeId,
    string MenteeName,
    string? MenteeGoal,
    int CompatibilityScore,
    DateTime RequestedAt);

/// <summary>
/// A card representing an active mentee on the mentor dashboard.
/// </summary>
public sealed record ActiveMenteeCardDto(
    Guid SessionId,
    Guid MenteeId,
    string MenteeName,
    string SessionTitle,
    string Status,
    int ProgressPercentage);

/// <summary>
/// Capacity indicator showing active/max mentees for the mentor.
/// </summary>
public sealed record CapacityIndicatorDto(
    int ActiveMentees,
    int MaxMentees,
    bool IsAtCapacity);

/// <summary>
/// Current availability status for the mentor dashboard toggle.
/// </summary>
public sealed record AvailabilityStatusDto(
    bool IsAvailable,
    string? UnavailabilityReason,
    DateTime? ReturnDate);
