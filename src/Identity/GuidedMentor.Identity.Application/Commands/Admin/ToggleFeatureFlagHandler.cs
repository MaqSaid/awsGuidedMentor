using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Handles enabling/disabling a feature flag. Updates AppConfig via IFeatureFlagService
/// and records the action in the audit log.
/// </summary>
public sealed class ToggleFeatureFlagHandler : IRequestHandler<ToggleFeatureFlagCommand, Result>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IAuditLogService _auditLogService;

    public ToggleFeatureFlagHandler(
        IAdminRepository adminRepository,
        IFeatureFlagService featureFlagService,
        IAuditLogService auditLogService)
    {
        _adminRepository = adminRepository;
        _featureFlagService = featureFlagService;
        _auditLogService = auditLogService;
    }

    public async Task<Result> Handle(ToggleFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var adminUserId = new UserId(request.AdminId);
        var admin = await _adminRepository.GetByLinkedUserIdAsync(adminUserId, cancellationToken);

        if (admin is null)
        {
            return Result.Failure("Admin account not found. Only Super Admins can perform this action.");
        }

        await _featureFlagService.SetFeatureEnabledAsync(
            request.FeatureName,
            request.Enabled,
            cancellationToken);

        var action = request.Enabled ? "EnableFeatureFlag" : "DisableFeatureFlag";

        await _auditLogService.RecordAsync(
            new AuditLogEntry(
                request.AdminId,
                DateTime.UtcNow,
                action,
                $"Feature:{request.FeatureName}",
                request.Reason),
            cancellationToken);

        return Result.Success();
    }
}
