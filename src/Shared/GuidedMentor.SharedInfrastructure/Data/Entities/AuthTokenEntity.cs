namespace GuidedMentor.SharedInfrastructure.Data.Entities;

/// <summary>
/// Persistence model for the auth_tokens table (magic links).
/// </summary>
public sealed class AuthTokenEntity
{
    public Guid Token { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Used { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
