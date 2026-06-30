using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Handles re-enabling a user account. Records the action in the audit log.
/// </summary>
public sealed class EnableUserHandler : IRequestHandler<EnableUserCommand, Result>
{
    private readonly IAdminRepository _adminRepository;
    private readonly GuidedMentor.Identity.Domain.Repositories.IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;

    public EnableUserHandler(
        IAdminRepository adminRepository,
        GuidedMentor.Identity.Domain.Repositories.IUserRepository userRepository,
        IAuditLogService auditLogService)
    {
        _adminRepository = adminRepository;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
    }

    public async Task<Result> Handle(EnableUserCommand request, CancellationToken cancellationToken)
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

        targetUser.Enable();

        await _userRepository.SaveAsync(targetUser, cancellationToken);

        await _auditLogService.RecordAsync(
            new AuditLogEntry(
                request.AdminId,
                DateTime.UtcNow,
                "EnableUser",
                $"User:{request.TargetUserId}",
                request.Reason),
            cancellationToken);

        return Result.Success();
    }
}
