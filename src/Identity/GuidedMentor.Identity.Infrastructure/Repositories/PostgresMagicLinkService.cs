using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.Identity.Infrastructure.Auth;
using GuidedMentor.SharedInfrastructure.Data;
using GuidedMentor.SharedInfrastructure.Data.Entities;
using GuidedMentor.SharedInfrastructure.Email;
using GuidedMentor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Identity.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL-backed magic link service. Creates tokens in auth_tokens table,
/// sends email via IEmailSender, verifies tokens, and issues JWT via JwtTokenService.
/// </summary>
public sealed class PostgresMagicLinkService : IMagicLinkService
{
    private readonly GuidedMentorDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<PostgresMagicLinkService> _logger;

    public PostgresMagicLinkService(
        GuidedMentorDbContext db,
        IEmailSender emailSender,
        JwtTokenService jwtTokenService,
        ILogger<PostgresMagicLinkService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<bool> CanSendAsync(string email, CancellationToken ct = default)
    {
        var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);
        var count = await _db.AuthTokens
            .CountAsync(t => t.Email == email && t.CreatedAt >= fifteenMinutesAgo, ct);

        return count < 3;
    }

    public async Task SendMagicLinkAsync(string email, CancellationToken ct = default)
    {
        var token = Guid.NewGuid();
        var entity = new AuthTokenEntity
        {
            Token = token,
            Email = email,
            Used = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _db.AuthTokens.Add(entity);
        await _db.SaveChangesAsync(ct);

        var magicLink = $"http://localhost:3000/auth/verify?email={Uri.EscapeDataString(email)}&token={token}";
        var htmlBody = $"""
            <h2>Your GuidedMentor Login Link</h2>
            <p>Click the link below to sign in:</p>
            <p><a href="{magicLink}">Sign in to GuidedMentor</a></p>
            <p>This link expires in 10 minutes.</p>
            <p>If you didn't request this, you can safely ignore this email.</p>
            """;

        try
        {
            await _emailSender.SendAsync(email, "Your GuidedMentor Magic Link", htmlBody, ct);
            _logger.LogInformation("Magic link sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send magic link email to {Email} — token still valid", email);
        }
    }

    public async Task<Result<AuthResponse>> VerifyAndAuthenticateAsync(string email, string token, CancellationToken ct = default)
    {
        if (!Guid.TryParse(token, out var tokenGuid))
        {
            return Result<AuthResponse>.Failure("Invalid token format.");
        }

        var authToken = await _db.AuthTokens.FindAsync([tokenGuid], ct);

        if (authToken is null)
        {
            return Result<AuthResponse>.Failure("Token not found.");
        }

        if (authToken.Email != email)
        {
            return Result<AuthResponse>.Failure("Token does not match email.");
        }

        if (authToken.Used)
        {
            return Result<AuthResponse>.Failure("Token has already been used.");
        }

        if (authToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Token has expired.");
        }

        // Mark token as used
        authToken.Used = true;
        await _db.SaveChangesAsync(ct);

        // Look up or create user
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = email.Split('@')[0],
                ActiveRole = null,
                MentorOnboardingStatus = "not_started",
                MenteeOnboardingStatus = "not_started",
                IsDisabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }

        if (user.IsDisabled)
        {
            return Result<AuthResponse>.Failure("Account is disabled.");
        }

        // Parse active role for token generation
        Role? activeRole = null;
        if (!string.IsNullOrEmpty(user.ActiveRole) &&
            Enum.TryParse<Role>(user.ActiveRole, true, out var parsedRole))
        {
            activeRole = parsedRole;
        }

        var response = _jwtTokenService.GenerateTokens(user.Id, user.Email, user.DisplayName, activeRole);
        _logger.LogInformation("User {Email} authenticated via magic link", email);

        return Result<AuthResponse>.Success(response);
    }
}
