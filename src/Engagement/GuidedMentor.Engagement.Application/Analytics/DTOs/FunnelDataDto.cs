namespace GuidedMentor.Engagement.Application.Analytics.DTOs;

/// <summary>
/// Response DTO for conversion funnel analysis.
/// Tracks user progression: signup → onboard → browse → match → session → complete.
///
/// Requirements: 30.6
/// </summary>
public sealed record FunnelDataDto(
    IReadOnlyList<FunnelStageDto> Stages,
    double OverallConversionRate);

/// <summary>
/// Represents a single stage in the conversion funnel.
/// </summary>
public sealed record FunnelStageDto(
    string StageName,
    int UserCount,
    double ConversionRateFromPrevious,
    double DropOffRate);
