using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GuidedMentor.LocalDev.Mocks;

/// <summary>
/// Mock authentication handler for local development.
/// Accepts any Bearer token in the format "dev-token-{userId}" and creates claims.
/// No real Cognito validation — purely for local testing.
/// </summary>
public sealed class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Default dev user if no token provided
        var userId = "11111111-1111-1111-1111-111111111111";
        var displayName = "Dev User";
        var role = "mentee";

        // Check for Authorization header
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var token = authHeader.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

            // Parse dev tokens: "dev-token-{userId}" or "dev-admin-token"
            if (token.StartsWith("dev-token-", StringComparison.OrdinalIgnoreCase))
            {
                userId = token["dev-token-".Length..];
            }
            else if (string.Equals(token, "dev-admin-token", StringComparison.OrdinalIgnoreCase))
            {
                userId = "admin-00000000-0000-0000-0000-000000000000";
                displayName = "Admin User";
                role = "admin";
            }
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Role, role),
            new Claim("cognito:groups", role == "admin" ? "Super_Admin" : "Users"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
