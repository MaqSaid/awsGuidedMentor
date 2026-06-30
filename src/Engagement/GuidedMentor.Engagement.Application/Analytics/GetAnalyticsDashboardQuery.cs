using GuidedMentor.Engagement.Application.Analytics.DTOs;
using MediatR;

namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Query to retrieve the operator analytics dashboard data.
/// Admin-only access — returns DAU/WAU/MAU, feature heatmap, error hotspots, and retention metrics.
///
/// Requirements: 30.4, 30.6
/// </summary>
public sealed record GetAnalyticsDashboardQuery() : IRequest<AnalyticsDashboardDto>;
