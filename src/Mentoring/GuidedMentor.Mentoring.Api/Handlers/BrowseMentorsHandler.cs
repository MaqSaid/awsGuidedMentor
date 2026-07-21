using GuidedMentor.Mentoring.Api.Endpoints;
using GuidedMentor.SharedInfrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GuidedMentor.Mentoring.Api.Handlers;

/// <summary>
/// Handles BrowseMentorsQuery — returns paginated list of available mentors with compatibility scores.
/// Uses deterministic scoring based on user IDs for local development.
/// Production will use the full matching algorithm.
/// </summary>
public sealed class BrowseMentorsHandler : IRequestHandler<BrowseMentorsQuery, BrowseMentorsResult>
{
    private readonly GuidedMentorDbContext _db;

    public BrowseMentorsHandler(GuidedMentorDbContext db)
    {
        _db = db;
    }

    public async Task<BrowseMentorsResult> Handle(BrowseMentorsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Mentors
            .Where(m => m.AvailabilityStatus != "unavailable")
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Chapter))
        {
            var chapterUsers = _db.Users.Where(u => u.AwsChapter == request.Chapter).Select(u => u.Id);
            query = query.Where(m => chapterUsers.Contains(m.UserId));
        }

        if (request.Skills is { Length: > 0 })
        {
            query = query.Where(m => m.ExpertiseAreas.Any(e => request.Skills.Contains(e)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var mentors = await query
            .OrderBy(m => m.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = mentors.Select(m => m.UserId).ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var cards = mentors.Select(m =>
        {
            var user = users.GetValueOrDefault(m.UserId);
            // Deterministic compatibility score based on mentor+mentee ID combination
            var hash = m.Id.GetHashCode() ^ request.MenteeUserId.GetHashCode();
            var score = 50 + (hash == int.MinValue ? 0 : Math.Abs(hash)) % 51;

            return new MentorCardDto(
                MentorId: m.Id,
                DisplayName: user?.DisplayName ?? "Unknown",
                Title: m.ProfessionalTitle ?? "AWS Professional",
                Chapter: user?.AwsChapter ?? "Sydney",
                ExpertiseAreas: m.ExpertiseAreas.ToList(),
                CompatibilityScore: score,
                HasActiveOpportunities: false,
                AvailabilityStatus: m.ActiveMenteeCount >= m.MaxMentees ? "at_capacity" : m.AvailabilityStatus);
        }).ToList();

        return new BrowseMentorsResult(cards, totalCount, request.Page, request.PageSize);
    }
}
