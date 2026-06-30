using GuidedMentor.Engagement.Application.DTOs;
using MediatR;

namespace GuidedMentor.Engagement.Application.Queries.Dashboard;

/// <summary>
/// Retrieves the aggregated mentee dashboard data including active sessions,
/// recommended mentors, progress stats, and summary bar.
/// </summary>
public sealed record GetMenteeDashboardQuery(Guid UserId) : IRequest<MenteeDashboardDto>;
