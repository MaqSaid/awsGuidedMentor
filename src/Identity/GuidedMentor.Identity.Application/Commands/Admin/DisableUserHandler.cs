using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Handles disabling a user account. Records the action in the audit log.
/// </summary>
public sealed class DisableUserHandler : IRequestHandler<DisableUserCommand, Result>
{
    private readonly IAdminRepository _adminRepository;
    private readonly GuidedMentor.Identity.Domain.Repositories.IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;

    public DisableUserHandler(
        IAdminRepository adminRepository,
        GuidedMentor.Identity.Domain.Repositories.IUserRepository userRepository,
        IAuditLogService auditLogService)
    {
        _adminRepository = adminRepository;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
    }

    public async Task<Result> Handle(DisableUserCommand request, CancellationToken cancellationToken)
    {
        var adminUserId = new UserId(request.AdminId);
        var admin = await _adminRepository.GetByLinkedUserIdAsync(adminUserId, cancellationToken);

        if (admin is null)
        {
            return Result.Failure("Admin account not found. Only Super Admins can perform this action.");
        }

        var targetUserId = new UserId(request.TargetUserId);
        var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);

        if (targetUser is null)
        {
            return Result.Failure("Target user not found.");
        }

        targetUser.Disable();

        await _userRepository.SaveAsync(targetUser, cancellationToken);

        await _auditLogService.RecordAsync(
            new AuditLogEntry(
                request.AdminId,
                DateTime.UtcNow,
                "DisableUser",
                $"User:{request.TargetUserId}",
                request.Reason),
            cancellationToken);

        return Result.Success();
    }
}
