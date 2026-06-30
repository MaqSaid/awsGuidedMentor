using GuidedMentor.Engagement.Application.Analytics.DTOs;
using MediatR;

namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Query to retrieve engagement-specific analytics metrics.
/// Tracks: browse-to-lock conversion, plan-to-completion rate, job view-to-click rate.
/// Admin-only access.
///
/// Requirements: 30.5
/// </summary>
public sealed record GetEngagementAnalyticsQuery(
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<EngagementAnalyticsDto>;
