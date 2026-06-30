using GuidedMentor.Engagement.Application.DTOs;
using MediatR;

namespace GuidedMentor.Engagement.Application.Queries.Dashboard;

/// <summary>
/// Retrieves the aggregated mentor dashboard data including pending requests,
/// active mentees, capacity indicator, and availability status.
/// </summary>
public sealed record GetMentorDashboardQuery(Guid UserId) : IRequest<MentorDashboardDto>;
