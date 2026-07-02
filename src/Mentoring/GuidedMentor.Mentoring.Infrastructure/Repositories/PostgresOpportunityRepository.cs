using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedInfrastructure.Data;
using GuidedMentor.SharedInfrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GuidedMentor.Mentoring.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of IOpportunityRepository for the Mentoring bounded context.
/// </summary>
public sealed class PostgresOpportunityRepository : IOpportunityRepository
{
    private readonly GuidedMentorDbContext _db;

    public PostgresOpportunityRepository(GuidedMentorDbContext db)
    {
        _db = db;
    }

    public async Task<OpportunityPosting?> GetByIdAsync(OpportunityPostingId id, CancellationToken ct = default)
    {
        var entity = await _db.Opportunities.FindAsync([id.Value], ct);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<int> GetActiveCountByMentorAsync(MentorId mentorId, CancellationToken ct = default)
    {
        return await _db.Opportunities
            .CountAsync(o => o.MentorId == mentorId.Value && o.IsActive, ct);
    }

    public async Task<IReadOnlyList<OpportunityPosting>> GetByMentorAsync(MentorId mentorId, CancellationToken ct = default)
    {
        var entities = await _db.Opportunities
            .Where(o => o.MentorId == mentorId.Value)
            .OrderByDescending(o => o.PublishedAt)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task SaveAsync(OpportunityPosting posting, CancellationToken ct = default)
    {
        var existing = await _db.Opportunities.FindAsync([posting.Id.Value], ct);

        if (existing is null)
        {
            var entity = MapToEntity(posting);
            _db.Opportunities.Add(entity);
        }
        else
        {
            existing.Title = posting.Title;
            existing.Type = posting.Type.ToString().ToLowerInvariant();
            existing.OrganisationName = posting.OrganisationName;
            existing.Description = posting.Description;
            existing.Location = posting.Location;
            existing.EventDateTime = posting.EventDateTime;
            existing.EmploymentType = posting.EmploymentType?.ToString().ToLowerInvariant();
            existing.RequiredSkills = posting.RequiredSkills.ToArray();
            existing.RequiredExperience = posting.RequiredExperience.ToString().ToLowerInvariant();
            existing.ExternalUrl = posting.ExternalUrl;
            existing.IsActive = posting.Status == PostingStatus.Active;
            existing.ExpiresAt = posting.ExpiresAt;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<OpportunityPosting> Items, int TotalCount)> BrowseAsync(
        OpportunityType? typeFilter,
        string? locationFilter,
        IReadOnlyList<string>? skillsFilter,
        ExperienceLevel? experienceFilter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Opportunities
            .Where(o => o.IsActive && (o.ExpiresAt == null || o.ExpiresAt > DateTime.UtcNow));

        if (typeFilter.HasValue)
        {
            var typeStr = typeFilter.Value.ToString().ToLowerInvariant();
            query = query.Where(o => o.Type == typeStr);
        }

        if (!string.IsNullOrEmpty(locationFilter))
        {
            query = query.Where(o => o.Location != null && o.Location.Contains(locationFilter));
        }

        if (experienceFilter.HasValue)
        {
            var expStr = experienceFilter.Value.ToString().ToLowerInvariant();
            query = query.Where(o => o.RequiredExperience == expStr);
        }

        var totalCount = await query.CountAsync(ct);

        var entities = await query
            .OrderByDescending(o => o.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = entities.Select(MapToDomain).ToList();

        // Apply skills filter in memory (array intersection)
        if (skillsFilter is not null && skillsFilter.Count > 0)
        {
            items = items
                .Where(p => p.RequiredSkills.Intersect(skillsFilter, StringComparer.OrdinalIgnoreCase).Any())
                .ToList();
        }

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<OpportunityPosting>> GetExpiredActivePostingsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entities = await _db.Opportunities
            .Where(o => o.IsActive && o.ExpiresAt != null && o.ExpiresAt < now)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    private static OpportunityPosting MapToDomain(OpportunityEntity entity)
    {
        var type = Enum.TryParse<OpportunityType>(entity.Type, true, out var t) ? t : OpportunityType.Job;
        var experience = Enum.TryParse<ExperienceLevel>(entity.RequiredExperience, true, out var e) ? e : ExperienceLevel.Beginner;
        var employment = !string.IsNullOrEmpty(entity.EmploymentType) && Enum.TryParse<EmploymentType>(entity.EmploymentType, true, out var emp)
            ? emp
            : (EmploymentType?)null;
        var status = entity.IsActive ? PostingStatus.Active : PostingStatus.Archived;

        return OpportunityPosting.Reconstitute(
            new OpportunityPostingId(entity.Id),
            new MentorId(entity.MentorId),
            entity.Title,
            type,
            entity.OrganisationName ?? string.Empty,
            entity.Description ?? string.Empty,
            entity.Location ?? string.Empty,
            entity.EventDateTime,
            employment,
            entity.RequiredSkills.ToList(),
            experience,
            entity.ExternalUrl ?? string.Empty,
            entity.PublishedAt,
            entity.ExpiresAt ?? entity.PublishedAt.AddDays(30),
            status);
    }

    private static OpportunityEntity MapToEntity(OpportunityPosting posting)
    {
        return new OpportunityEntity
        {
            Id = posting.Id.Value,
            MentorId = posting.PostedByMentorId.Value,
            Title = posting.Title,
            Type = posting.Type.ToString().ToLowerInvariant(),
            OrganisationName = posting.OrganisationName,
            Description = posting.Description,
            Location = posting.Location,
            EventDateTime = posting.EventDateTime,
            EmploymentType = posting.EmploymentType?.ToString().ToLowerInvariant(),
            RequiredSkills = posting.RequiredSkills.ToArray(),
            RequiredExperience = posting.RequiredExperience.ToString().ToLowerInvariant(),
            ExternalUrl = posting.ExternalUrl,
            IsActive = posting.Status == PostingStatus.Active,
            PublishedAt = posting.PublishedAt,
            ExpiresAt = posting.ExpiresAt
        };
    }
}
