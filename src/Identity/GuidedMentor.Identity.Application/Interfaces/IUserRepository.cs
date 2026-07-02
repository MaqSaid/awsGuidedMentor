namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Repository interface for user persistence operations.
/// Implementation lives in the Infrastructure layer.
/// </summary>
public interface IUserRepository
{
    Task<string?> GetUserIdByEmailAsync(string email, CancellationToken ct);
}
