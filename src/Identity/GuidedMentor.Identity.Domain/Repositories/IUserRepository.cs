using GuidedMentor.Identity.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for the User aggregate. Defined in Domain layer,
/// implemented in Infrastructure layer.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);
    Task SaveAsync(User user, CancellationToken ct = default);
    Task<bool> ExistsAsync(Email email, CancellationToken ct = default);
}
