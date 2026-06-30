using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Handles toggling maintenance mode. Stores the flag via IFeatureFlagService
/// and records the action in the audit log.
/// </summary>
public sealed class SetMaintenanceModeHandler : IRequestHandler<SetMaintenanceModeCommand, Result>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IAuditLogService _auditLogService;

    public SetMaintenanceModeHandler(
        IAdminRepository adminRepository,
        IFeatureFlagService featureFlagService,
        IAuditLogService auditLogService)
    {
        _adminRepository = adminRepository;
        _featureFlagService = featureFlagService;
        _auditLogService = auditLogService;
    }

    public async Task<Result> Handle(SetMaintenanceModeCommand request, CancellationToken cancellationToken)
    {
        var adminUserId = new UserId(request.AdminId);
        var admin = await _adminRepository.GetByLinkedUserIdAsync(adminUserId, cancellationToken);

        if (admin is null)
        {
            return Result.Failure("Admin account not found. Only Super Admins can perform this action.");
        }

        await _featureFlagService.SetMaintenanceModeAsync(
            request.Enabled,
            request.EstimatedReturnTime,
            cancellationToken);

        var action = request.Enabled ? "EnableMaintenanceMode" : "DisableMaintenanceMode";

        await _auditLogService.RecordAsync(
            new AuditLogEntry(
                request.AdminId,
                DateTime.UtcNow,
                action,
                "Platform:MaintenanceMode",
                request.Reason),
            cancellationToken);

        return Result.Success();
    }
}
