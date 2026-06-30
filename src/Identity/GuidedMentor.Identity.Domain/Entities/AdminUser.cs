using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Domain.Entities;

/// <summary>
/// Represents a Super Admin account. MFA is always enforced.
/// The platform supports a maximum of 5 Super Admin accounts.
/// </summary>
public sealed class AdminUser : Entity<AdminUserId>
{
    public const int MaxAdminAccounts = 5;

    public UserId LinkedUserId { get; private set; } = null!;
    public string AdminEmail { get; private set; } = string.Empty;
    public bool IsMfaEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Required for ORM/deserialization
    private AdminUser() { }

    /// <summary>
    /// Factory method to create a new AdminUser. MFA is always enabled.
    /// </summary>
    public static Result<AdminUser> Create(
        AdminUserId id,
        UserId linkedUserId,
        string adminEmail,
        int currentAdminCount)
    {
        if (currentAdminCount >= MaxAdminAccounts)
        {
            return Result<AdminUser>.Failure(
                $"Cannot create admin account. Maximum of {MaxAdminAccounts} Super Admin accounts reached.");
        }

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            return Result<AdminUser>.Failure("Admin email is required.");
        }

        var admin = new AdminUser
        {
            Id = id,
            LinkedUserId = linkedUserId,
            AdminEmail = adminEmail.Trim().ToLowerInvariant(),
            IsMfaEnabled = true, // Always enforced
            CreatedAt = DateTime.UtcNow
        };

        return Result<AdminUser>.Success(admin);
    }
}
