using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedInfrastructure.Data;
using GuidedMentor.SharedInfrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GuidedMentor.Mentoring.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of ISessionRepository for the Mentoring bounded context.
/// </summary>
public sealed class PostgresSessionRepository : ISessionRepository
{
    private readonly GuidedMentorDbContext _db;

    public PostgresSessionRepository(GuidedMentorDbContext db)
    {
        _db = db;
    }

    public async Task CreatePendingSessionAsync(
        SessionId sessionId,
        MenteeId menteeId,
        MentorId mentorId,
        LockId lockId,
        CancellationToken cancellationToken = default)
    {
        var entity = new SessionEntity
        {
            Id = sessionId.Value,
            MenteeId = menteeId.Value,
            MentorId = mentorId.Value,
            Status = "pending_acceptance",
            LockId = lockId.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Sessions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Session?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Sessions.FindAsync([sessionId.Value], cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task SaveAsync(Session session, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Sessions.FindAsync([session.Id.Value], cancellationToken);
        if (entity is null) return;

        entity.Status = MapStatusToString(session.Status);
        entity.MenteeCompletedAt = session.MenteeCompletedAt;
        entity.MentorCompletedAt = session.MentorCompletedAt;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Sessions.FindAsync([sessionId.Value], cancellationToken);
        if (entity is not null)
        {
            _db.Sessions.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Session>> GetSessionsAwaitingConfirmationAsync(
        int menteeCompletedDaysAgo,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-menteeCompletedDaysAgo);
        var entities = await _db.Sessions
            .Where(s => s.Status == "mentee_completed" && s.MenteeCompletedAt != null && s.MenteeCompletedAt < cutoff)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    private static Session MapToDomain(SessionEntity entity)
    {
        var status = ParseStatus(entity.Status);

        return Session.Reconstitute(
            new SessionId(entity.Id),
            new MenteeId(entity.MenteeId),
            new MentorId(entity.MentorId),
            new LockId(entity.LockId ?? Guid.Empty),
            status,
            entity.MenteeCompletedAt,
            entity.MentorCompletedAt,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static SessionStatus ParseStatus(string status) => status switch
    {
        "pending_acceptance" => SessionStatus.PendingAcceptance,
        "pending_plan" => SessionStatus.PendingPlan,
        "active" => SessionStatus.Active,
        "mentee_completed" => SessionStatus.MenteeCompleted,
        "completed" => SessionStatus.Completed,
        "unresolved" => SessionStatus.Unresolved,
        _ => SessionStatus.PendingAcceptance
    };

    private static string MapStatusToString(SessionStatus status) => status switch
    {
        SessionStatus.PendingAcceptance => "pending_acceptance",
        SessionStatus.PendingPlan => "pending_plan",
        SessionStatus.Active => "active",
        SessionStatus.MenteeCompleted => "mentee_completed",
        SessionStatus.Completed => "completed",
        SessionStatus.Unresolved => "unresolved",
        _ => "pending_acceptance"
    };
}
