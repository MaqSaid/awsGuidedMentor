namespace GuidedMentor.SharedInfrastructure.Security;

/// <summary>
/// Configuration options for the security middleware stack.
/// Bind from appsettings.json section "Security".
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Cognito User Pool region (e.g., "ap-southeast-2").
    /// </summary>
    public string CognitoRegion { get; set; } = "ap-southeast-2";

    /// <summary>
    /// Cognito User Pool ID for JWT issuer validation.
    /// </summary>
    public string CognitoUserPoolId { get; set; } = string.Empty;

    /// <summary>
    /// Cognito App Client ID for audience validation.
    /// </summary>
    public string CognitoClientId { get; set; } = string.Empty;

    /// <summary>
    /// Allowed frontend origins for CORS/CSRF validation.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = ["https://guidedmentor.dev", "http://localhost:5173"];

    /// <summary>
    /// Rate limit: maximum requests per window per authenticated user.
    /// </summary>
    public int RateLimitMaxRequests { get; set; } = 100;

    /// <summary>
    /// Rate limit: sliding window duration in seconds.
    /// </summary>
    public int RateLimitWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum request body size in bytes. Default is 1 MB.
    /// </summary>
    public long MaxRequestBodySizeBytes { get; set; } = 1_048_576;

    /// <summary>
    /// HSTS max-age in seconds. Default is 1 year (31536000).
    /// </summary>
    public int HstsMaxAgeSeconds { get; set; } = 31_536_000;

    /// <summary>
    /// Content Security Policy directive value.
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self' https://*.amazonaws.com https://*.amazoncognito.com; frame-ancestors 'none';";

    /// <summary>
    /// Paths that are exempt from JWT authentication (e.g., health checks).
    /// </summary>
    public string[] AnonymousPaths { get; set; } = ["/v1/health", "/v1/auth/signup/google", "/v1/auth/signup/email", "/v1/auth/signin", "/v1/auth/verify-email", "/v1/auth/refresh"];

    /// <summary>
    /// Computed Cognito issuer URL.
    /// </summary>
    public string CognitoIssuer => $"https://cognito-idp.{CognitoRegion}.amazonaws.com/{CognitoUserPoolId}";

    /// <summary>
    /// Computed JWKS URI for token signature validation.
    /// </summary>
    public string JwksUri => $"{CognitoIssuer}/.well-known/jwks.json";
}
