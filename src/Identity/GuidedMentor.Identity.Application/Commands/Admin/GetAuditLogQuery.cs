using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Retrieves paginated audit log entries with optional filters for date range,
/// action type, and admin ID.
/// </summary>
public sealed record GetAuditLogQuery(
    Guid AdminId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? ActionType,
    Guid? FilterAdminId,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<AuditLogResultDto>>;

/// <summary>
/// Paginated result of audit log entries.
/// </summary>
public sealed record AuditLogResultDto(
    IReadOnlyList<AuditLogEntry> Entries,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

/// <summary>
/// Handles the GetAuditLogQuery. Verifies the requester is a Super Admin
/// and returns filtered, paginated audit log entries.
/// </summary>
public sealed class GetAuditLogHandler : IRequestHandler<GetAuditLogQuery, Result<AuditLogResultDto>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogService _auditLogService;

    public GetAuditLogHandler(
        IAdminRepository adminRepository,
        IAuditLogService auditLogService)
    {
        _adminRepository = adminRepository;
        _auditLogService = auditLogService;
    }

    public async Task<Result<AuditLogResultDto>> Handle(
        GetAuditLogQuery request,
        CancellationToken cancellationToken)
    {
        var adminUserId = new UserId(request.AdminId);
        var admin = await _adminRepository.GetByLinkedUserIdAsync(adminUserId, cancellationToken);

        if (admin is null)
        {
            return Result<AuditLogResultDto>.Failure(
                "Admin account not found. Only Super Admins can view the audit log.");
        }

        var allEntries = await _auditLogService.GetRecentAsync(1000, cancellationToken);

        // Apply filters
        var filtered = allEntries.AsEnumerable();

        if (request.StartDate.HasValue)
        {
            filtered = filtered.Where(e => e.Timestamp >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            filtered = filtered.Where(e => e.Timestamp <= request.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            filtered = filtered.Where(e =>
                e.Action.Equals(request.ActionType, StringComparison.OrdinalIgnoreCase));
        }

        if (request.FilterAdminId.HasValue)
        {
            filtered = filtered.Where(e => e.AdminId == request.FilterAdminId.Value);
        }

        var filteredList = filtered.OrderByDescending(e => e.Timestamp).ToList();
        var totalCount = filteredList.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var pagedEntries = filteredList
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result<AuditLogResultDto>.Success(
            new AuditLogResultDto(
                pagedEntries,
                totalCount,
                request.Page,
                request.PageSize,
                totalPages));
    }
}
