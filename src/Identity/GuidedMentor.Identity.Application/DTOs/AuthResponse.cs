using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.DTOs;

/// <summary>
/// Response DTO for authentication operations that issue tokens.
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string IdToken,
    Role? ActiveRole,
    int ExpiresIn);
