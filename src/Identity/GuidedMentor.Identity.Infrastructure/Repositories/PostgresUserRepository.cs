using GuidedMentor.Identity.Domain.Entities;
using GuidedMentor.SharedKernel;
using GuidedMentor.SharedInfrastructure.Data;
using GuidedMentor.SharedInfrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using DomainIUserRepository = GuidedMentor.Identity.Domain.Repositories.IUserRepository;
using AppIUserRepository = GuidedMentor.Identity.Application.Interfaces.IUserRepository;

namespace GuidedMentor.Identity.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of the User repository.
/// Implements both Domain and Application layer interfaces.
/// </summary>
public sealed class PostgresUserRepository : DomainIUserRepository, AppIUserRepository
{
    private readonly GuidedMentorDbContext _db;

    public PostgresUserRepository(GuidedMentorDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
    {
        var entity = await _db.Users.FindAsync([id.Value], ct);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
    {
        var entity = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email.Value, ct);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task SaveAsync(User user, CancellationToken ct = default)
    {
        var existing = await _db.Users.FindAsync([user.Id.Value], ct);

        if (existing is null)
        {
            var entity = MapToEntity(user);
            _db.Users.Add(entity);
        }
        else
        {
            existing.Email = user.Email.Value;
            existing.DisplayName = user.DisplayName;
            existing.ProfilePhotoUrl = user.ProfilePhotoUrl;
            existing.AwsChapter = user.AwsChapter.ToString();
            existing.City = user.City;
            existing.ActiveRole = user.ActiveRole?.ToString().ToLowerInvariant();
            existing.MentorOnboardingStatus = MapOnboardingStatus(user.MentorOnboardingStatus);
            existing.MenteeOnboardingStatus = MapOnboardingStatus(user.MenteeOnboardingStatus);
            existing.IsDisabled = user.IsDisabled;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Email email, CancellationToken ct = default)
    {
        return await _db.Users.AnyAsync(u => u.Email == email.Value, ct);
    }

    public async Task<string?> GetUserIdByEmailAsync(string email, CancellationToken ct)
    {
        var entity = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);
        return entity?.Id.ToString();
    }

    private static User MapToDomain(UserEntity entity)
    {
        var userId = new UserId(entity.Id);
        var emailResult = Email.Create(entity.Email);
        var email = emailResult.Value;

        var chapter = Enum.TryParse<AustralianChapter>(entity.AwsChapter, true, out var ch)
            ? ch
            : AustralianChapter.Sydney;

        var user = User.Create(
            userId,
            email,
            entity.DisplayName,
            chapter,
            entity.City ?? string.Empty,
            entity.ProfilePhotoUrl);

        // Set role if present
        if (!string.IsNullOrEmpty(entity.ActiveRole) &&
            Enum.TryParse<Role>(entity.ActiveRole, true, out var role))
        {
            user.SetInitialRole(role);
        }

        // Set onboarding statuses
        if (Enum.TryParse<OnboardingStatus>(entity.MentorOnboardingStatus?.Replace("_", ""), true, out var mentorStatus))
        {
            user.SetOnboardingStatus(Role.Mentor, mentorStatus);
        }

        if (Enum.TryParse<OnboardingStatus>(entity.MenteeOnboardingStatus?.Replace("_", ""), true, out var menteeStatus))
        {
            user.SetOnboardingStatus(Role.Mentee, menteeStatus);
        }

        if (entity.IsDisabled)
        {
            user.Disable();
        }

        user.ClearDomainEvents();
        return user;
    }

    private static UserEntity MapToEntity(User user)
    {
        return new UserEntity
        {
            Id = user.Id.Value,
            Email = user.Email.Value,
            DisplayName = user.DisplayName,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            AwsChapter = user.AwsChapter.ToString(),
            City = user.City,
            ActiveRole = user.ActiveRole?.ToString().ToLowerInvariant(),
            MentorOnboardingStatus = MapOnboardingStatus(user.MentorOnboardingStatus),
            MenteeOnboardingStatus = MapOnboardingStatus(user.MenteeOnboardingStatus),
            IsDisabled = user.IsDisabled,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private static string MapOnboardingStatus(OnboardingStatus status) => status switch
    {
        OnboardingStatus.NotStarted => "not_started",
        OnboardingStatus.InProgress => "in_progress",
        OnboardingStatus.Completed => "completed",
        _ => "not_started"
    };
}
