using System.Text.Json;
using GuidedMentor.Content.Application.Interfaces;
using GuidedMentor.Content.Domain;
using GuidedMentor.SharedInfrastructure.Data;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Content.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of ISessionPlanRepository.
/// Stores session plans as JSONB in the sessions.session_plan column.
/// </summary>
public sealed class PostgresSessionPlanRepository : ISessionPlanRepository
{
    private readonly GuidedMentorDbContext _db;
    private readonly ILogger<PostgresSessionPlanRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public PostgresSessionPlanRepository(GuidedMentorDbContext db, ILogger<PostgresSessionPlanRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SavePlanAsync(Guid sessionId, SessionPlan plan, string bedrockModelVersion, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Sessions.FindAsync([sessionId], cancellationToken);
        if (entity is null)
        {
            _logger.LogWarning("Session {SessionId} not found when saving plan", sessionId);
            return;
        }

        var planJson = new
        {
            plan.SessionTitle,
            Agenda = plan.Agenda.Select(a => new { a.Title, a.DurationMinutes, a.Description }),
            plan.PreworkTasks,
            plan.FollowUpTasks,
            GeneratedBy = bedrockModelVersion,
            GeneratedAt = DateTime.UtcNow
        };

        entity.SessionPlan = JsonSerializer.Serialize(planJson, JsonOptions);
        entity.Status = "active";
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Session plan saved for session {SessionId}", sessionId);
    }

    public async Task SetPendingPlanStatusAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Sessions.FindAsync([sessionId], cancellationToken);
        if (entity is null) return;

        entity.Status = "pending_plan";
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateReviewStatusAsync(
        Guid sessionId,
        Guid reviewedByAdminId,
        string reviewStatus,
        string? flagReason,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Sessions.FindAsync([sessionId], cancellationToken);
        if (entity is null) return;

        // Parse existing plan JSON and add review metadata
        var existingPlan = string.IsNullOrEmpty(entity.SessionPlan)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(entity.SessionPlan, JsonOptions)
              ?? new Dictionary<string, object>();

        existingPlan["reviewStatus"] = reviewStatus;
        existingPlan["reviewedBy"] = reviewedByAdminId.ToString();
        existingPlan["reviewedAt"] = DateTime.UtcNow.ToString("O");

        if (!string.IsNullOrEmpty(flagReason))
        {
            existingPlan["flagReason"] = flagReason;
        }

        entity.SessionPlan = JsonSerializer.Serialize(existingPlan, JsonOptions);
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
