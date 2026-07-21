using GuidedMentor.Mentoring.Api.Endpoints;
using GuidedMentor.SharedInfrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GuidedMentor.Mentoring.Api.Handlers;

/// <summary>
/// Handles GetMentorDetailQuery — returns detailed mentor profile with compatibility score.
/// </summary>
public sealed class GetMentorDetailHandler : IRequestHandler<GetMentorDetailQuery, MentorDetailDto?>
{
    private readonly GuidedMentorDbContext _db;

    public GetMentorDetailHandler(GuidedMentorDbContext db)
    {
        _db = db;
    }

    public async Task<MentorDetailDto?> Handle(GetMentorDetailQuery request, CancellationToken cancellationToken)
    {
        var mentor = await _db.Mentors.FindAsync([request.MentorId], cancellationToken);
        if (mentor is null) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == mentor.UserId, cancellationToken);
        if (user is null) return null;

        var hash = mentor.Id.GetHashCode() ^ request.MenteeUserId.GetHashCode();
        var score = 50 + (hash == int.MinValue ? 0 : Math.Abs(hash)) % 51;

        return new MentorDetailDto(
            MentorId: mentor.Id,
            DisplayName: user.DisplayName,
            Title: mentor.ProfessionalTitle ?? "AWS Professional",
            Company: mentor.CompanyName ?? "",
            Chapter: user.AwsChapter ?? "Sydney",
            City: user.City ?? "",
            Bio: mentor.Bio ?? "",
            ExpertiseAreas: mentor.ExpertiseAreas.ToList(),
            Certifications: mentor.Certifications.ToList(),
            CompatibilityScore: score,
            ActiveMenteeCount: mentor.ActiveMenteeCount,
            MaxMentees: mentor.MaxMentees,
            HasActiveOpportunities: false,
            AvailabilityStatus: mentor.ActiveMenteeCount >= mentor.MaxMentees ? "at_capacity" : mentor.AvailabilityStatus);
    }
}
