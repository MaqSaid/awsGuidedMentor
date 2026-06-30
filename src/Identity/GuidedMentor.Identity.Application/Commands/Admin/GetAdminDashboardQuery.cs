using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Retrieves the admin dashboard data: user counts, session counts,
/// platform health status, and recent audit log entries.
/// </summary>
public sealed record GetAdminDashboardQuery(Guid AdminId) : IRequest<Result<AdminDashboardDto>>;

/// <summary>
/// DTO returned by GetAdminDashboardQuery.
/// </summary>
public sealed record AdminDashboardDto(
    int TotalUserCount,
    int MentorCount,
    int MenteeCount,
    int ActiveSessionCount,
    string PlatformHealthStatus,
    IReadOnlyList<AuditLogEntry> RecentAuditEntries);

/// <summary>
/// Handles the GetAdminDashboardQuery. Verifies the requester is a Super Admin
/// and aggregates dashboard metrics.
/// </summary>
public sealed class GetAdminDashboardHandler : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardDto>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAdminDashboardDataService _dashboardDataService;
    private readonly IAuditLogService _auditLogService;

    public GetAdminDashboardHandler(
        IAdminRepository adminRepository,
        IAdminDashboardDataService dashboardDataService,
        IAuditLogService auditLogService)
    {
        _adminRepository = adminRepository;
        _dashboardDataService = dashboardDataService;
        _auditLogService = auditLogService;
    }

    public async Task<Result<AdminDashboardDto>> Handle(
        GetAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var adminUserId = new UserId(request.AdminId);
        var admin = await _adminRepository.GetByLinkedUserIdAsync(adminUserId, cancellationToken);

        if (admin is null)
        {
            return Result<AdminDashboardDto>.Failure(
                "Admin account not found. Only Super Admins can access the dashboard.");
        }

        var totalUsers = await _dashboardDataService.GetTotalUserCountAsync(cancellationToken);
        var mentorCount = await _dashboardDataService.GetUserCountByRoleAsync("mentor", cancellationToken);
        var menteeCount = await _dashboardDataService.GetUserCountByRoleAsync("mentee", cancellationToken);
        var activeSessions = await _dashboardDataService.GetActiveSessionCountAsync(cancellationToken);
        var healthStatus = await _dashboardDataService.GetPlatformHealthStatusAsync(cancellationToken);
        var recentAudit = await _auditLogService.GetRecentAsync(20, cancellationToken);

        return Result<AdminDashboardDto>.Success(
            new AdminDashboardDto(
                totalUsers,
                mentorCount,
                menteeCount,
                activeSessions,
                healthStatus,
                recentAudit));
    }
}
