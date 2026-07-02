namespace GuidedMentor.Identity.Infrastructure.Auth;

/// <summary>
/// Configuration options for self-hosted JWT token generation.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = "super-secret-key-for-dev-at-least-32-chars-long!!";
    public string Issuer { get; set; } = "GuidedMentor";
    public string Audience { get; set; } = "GuidedMentor";
}
