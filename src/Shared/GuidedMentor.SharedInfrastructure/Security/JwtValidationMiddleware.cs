using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace GuidedMentor.SharedInfrastructure.Security;

/// <summary>
/// Middleware that validates JWT access tokens issued by Amazon Cognito.
/// Expects tokens with 15-minute expiry issued by the configured Cognito User Pool.
/// Populates HttpContext.User with claims from the validated token.
/// Returns 401 Unauthorized for missing or invalid tokens.
/// </summary>
public sealed class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;
    private readonly ILogger<JwtValidationMiddleware> _logger;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtValidationMiddleware(
        RequestDelegate next,
        IOptions<SecurityOptions> options,
        ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;

        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            _options.JwksUri.Replace("/.well-known/jwks.json", "/.well-known/openid-configuration"),
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });

        _tokenHandler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip authentication for anonymous paths
        if (IsAnonymousPath(path))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Missing or invalid Authorization header for path {Path}", path);
            await WriteUnauthorizedResponse(context, "Authentication is required to access this resource.");
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var openIdConfig = await _configManager.GetConfigurationAsync(context.RequestAborted);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.CognitoIssuer,
                ValidateAudience = true,
                ValidAudience = _options.CognitoClientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                await WriteUnauthorizedResponse(context, "Invalid token format.");
                return;
            }

            // Validate token_use claim is "access" (not "id" token)
            var tokenUse = jwtToken.Claims.FirstOrDefault(c => c.Type == "token_use")?.Value;
            if (tokenUse != "access")
            {
                _logger.LogWarning("Token is not an access token. token_use={TokenUse}", tokenUse);
                await WriteUnauthorizedResponse(context, "Invalid token type.");
                return;
            }

            // Set the authenticated user on the context
            context.User = principal;

            // Store userId (sub claim) in HttpContext.Items for easy access
            var userId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                context.Items["UserId"] = userId;
            }

            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogInformation("Expired JWT token for path {Path}", path);
            await WriteUnauthorizedResponse(context, "Token has expired.");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid JWT token for path {Path}", path);
            await WriteUnauthorizedResponse(context, "Invalid token.");
        }
    }

    private bool IsAnonymousPath(string path)
    {
        return _options.AnonymousPaths.Any(p =>
            path.Equals(p, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            statusCode = 401,
            error = "Unauthorized",
            message,
            correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id?.ToString() : Guid.NewGuid().ToString("D")
        });
    }
}
