using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.ValueObjects;
using GuidedMentor.SharedInfrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuidedMentor.Mentoring.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of IMenteeRepository for the Mentoring bounded context.
/// </summary>
public sealed class PostgresMenteeRepository : IMenteeRepository
{
    private readonly GuidedMentorDbContext _db;

    public PostgresMenteeRepository(GuidedMentorDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Guid>> GetMenteeIdsWithActiveSessionsForMentorAsync(
        Guid mentorId, CancellationToken ct = default)
    {
        var menteeIds = await _db.Sessions
            .Where(s => s.MentorId == mentorId && s.Status == "active")
            .Select(s => s.MenteeId)
            .Distinct()
            .ToListAsync(ct);

        // Map mentee IDs back to user IDs for notifications
        var userIds = await _db.Mentees
            .Where(m => menteeIds.Contains(m.Id))
            .Select(m => m.UserId)
            .ToListAsync(ct);

        return userIds;
    }

    public async Task<IReadOnlyList<Guid>> GetSkillMatchedMenteeIdsAsync(
        IReadOnlyList<string> requiredSkills, int minimumOverlap, CancellationToken ct = default)
    {
        // Load mentees and filter in memory for array overlap (PostgreSQL array operations)
        var mentees = await _db.Mentees.ToListAsync(ct);

        var matched = mentees
            .Where(m => m.Skills.Intersect(requiredSkills, StringComparer.OrdinalIgnoreCase).Count() >= minimumOverlap)
            .Select(m => m.UserId)
            .ToList();

        return matched;
    }

    public Task<OpportunityNotificationPreferences?> GetOpportunityPreferencesAsync(
        Guid menteeId, CancellationToken ct = default)
    {
        // Default preferences for all mentees (stored preferences not yet implemented at DB level)
        return Task.FromResult<OpportunityNotificationPreferences?>(
            OpportunityNotificationPreferences.Default());
    }

    public Task SaveOpportunityPreferencesAsync(
        Guid menteeId, OpportunityNotificationPreferences preferences, CancellationToken ct = default)
    {
        // Preferences stored as default for now — future: store in mentees table JSONB column
        return Task.CompletedTask;
    }
}
