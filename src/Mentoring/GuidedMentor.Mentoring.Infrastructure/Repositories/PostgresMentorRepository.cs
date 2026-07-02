using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.ValueObjects;
using GuidedMentor.SharedInfrastructure.Data;
using GuidedMentor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using DomainMentor = GuidedMentor.Mentoring.Domain.Entities.MentorEntity;
using EfMentor = GuidedMentor.SharedInfrastructure.Data.Entities.MentorEntity;

namespace GuidedMentor.Mentoring.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of IMentorRepository for the Mentoring bounded context.
/// </summary>
public sealed class PostgresMentorRepository : IMentorRepository
{
    private readonly GuidedMentorDbContext _db;

    public PostgresMentorRepository(GuidedMentorDbContext db)
    {
        _db = db;
    }

    public async Task<DomainMentor?> GetByIdAsync(Guid mentorId, CancellationToken ct = default)
    {
        var entity = await _db.Mentors.FindAsync([mentorId], ct);
        if (entity is null) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId, ct);
        return MapToDomain(entity, user);
    }

    public async Task SaveAvailabilityAsync(DomainMentor mentor, CancellationToken ct = default)
    {
        var entity = await _db.Mentors.FindAsync([mentor.Id], ct);
        if (entity is null) return;

        entity.AvailabilityStatus = mentor.Availability.Status.ToString().ToLowerInvariant();
        entity.UnavailabilityReason = mentor.Availability.Reason?.ToString().ToLowerInvariant();
        entity.ReturnDate = mentor.Availability.ReturnDate;
        entity.UnavailableSince = mentor.Availability.UnavailableSince;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<MentorAvailability?> GetAvailabilityAsync(Guid mentorId, CancellationToken ct = default)
    {
        var entity = await _db.Mentors.FindAsync([mentorId], ct);
        if (entity is null) return null;

        return MapAvailability(entity);
    }

    public async Task<IReadOnlyList<DomainMentor>> GetUnavailableMentorsOverDaysAsync(int days, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var entities = await _db.Mentors
            .Where(m => m.AvailabilityStatus == "unavailable" && m.UnavailableSince != null && m.UnavailableSince < cutoff)
            .ToListAsync(ct);

        var results = new List<DomainMentor>();
        foreach (var entity in entities)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == entity.UserId, ct);
            results.Add(MapToDomain(entity, user));
        }

        return results;
    }

    public async Task IncrementActiveMenteeCountAsync(Guid mentorId, CancellationToken ct = default)
    {
        var entity = await _db.Mentors.FindAsync([mentorId], ct);
        if (entity is null) return;

        entity.ActiveMenteeCount++;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DecrementActiveMenteeCountAsync(Guid mentorId, CancellationToken ct = default)
    {
        var entity = await _db.Mentors.FindAsync([mentorId], ct);
        if (entity is null) return;

        if (entity.ActiveMenteeCount > 0)
        {
            entity.ActiveMenteeCount--;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveSettingsAsync(DomainMentor mentor, CancellationToken ct = default)
    {
        var entity = await _db.Mentors.FindAsync([mentor.Id], ct);
        if (entity is null) return;

        entity.MaxMentees = mentor.MaxMentees;
        entity.AvailabilityStatus = mentor.Availability.Status.ToString().ToLowerInvariant();
        entity.UnavailabilityReason = mentor.Availability.Reason?.ToString().ToLowerInvariant();
        entity.ReturnDate = mentor.Availability.ReturnDate;
        entity.UnavailableSince = mentor.Availability.UnavailableSince;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static DomainMentor MapToDomain(EfMentor entity, SharedInfrastructure.Data.Entities.UserEntity? user)
    {
        var availability = MapAvailability(entity);

        AustralianChapter? chapter = null;
        if (user is not null && !string.IsNullOrEmpty(user.AwsChapter) &&
            Enum.TryParse<AustralianChapter>(user.AwsChapter, true, out var ch))
        {
            chapter = ch;
        }

        return DomainMentor.Create(
            entity.Id,
            user?.DisplayName ?? "Unknown",
            entity.MaxMentees,
            entity.ActiveMenteeCount,
            availability,
            chapter);
    }

    private static MentorAvailability MapAvailability(EfMentor entity)
    {
        var status = entity.AvailabilityStatus == "unavailable"
            ? AvailabilityStatus.Unavailable
            : AvailabilityStatus.Available;

        UnavailabilityReason? reason = null;
        if (!string.IsNullOrEmpty(entity.UnavailabilityReason) &&
            Enum.TryParse<UnavailabilityReason>(entity.UnavailabilityReason, true, out var r))
        {
            reason = r;
        }

        return MentorAvailability.FromPersisted(status, reason, entity.ReturnDate, entity.UnavailableSince);
    }
}
