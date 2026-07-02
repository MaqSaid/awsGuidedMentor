using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.SharedKernel;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GuidedMentor.Identity.Infrastructure.Auth;

/// <summary>
/// Self-hosted JWT token generation service. Replaces AWS Cognito token issuance.
/// </summary>
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public AuthResponse GenerateTokens(Guid userId, string email, string? displayName, Role? activeRole)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("name", displayName ?? email.Split('@')[0]),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (activeRole is not null)
        {
            claims.Add(new Claim("role", activeRole.Value.ToString().ToLowerInvariant()));
        }

        var accessToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        var refreshToken = Guid.NewGuid().ToString();
        var tokenHandler = new JwtSecurityTokenHandler();

        return new AuthResponse(
            tokenHandler.WriteToken(accessToken),
            refreshToken,
            tokenHandler.WriteToken(accessToken),
            activeRole,
            900);
    }
}
