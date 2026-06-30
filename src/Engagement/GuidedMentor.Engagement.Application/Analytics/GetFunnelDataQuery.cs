using GuidedMentor.Engagement.Application.Analytics.DTOs;
using MediatR;

namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Query to retrieve conversion funnel data for the operator analytics dashboard.
/// Tracks user progression: signup → onboard → browse → match → session → complete.
/// Admin-only access.
///
/// Requirements: 30.6
/// </summary>
public sealed record GetFunnelDataQuery() : IRequest<FunnelDataDto>;
