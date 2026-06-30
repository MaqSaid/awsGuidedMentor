using GuidedMentor.Identity.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for the AdminUser entity.
/// Supports CRUD operations and count checks for the max 5 admin limit.
/// </summary>
public interface IAdminRepository
{
    Task<AdminUser?> GetByIdAsync(AdminUserId id, CancellationToken ct = default);
    Task<AdminUser?> GetByLinkedUserIdAsync(UserId userId, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
    Task SaveAsync(AdminUser admin, CancellationToken ct = default);
    Task DeleteAsync(AdminUserId id, CancellationToken ct = default);
}
