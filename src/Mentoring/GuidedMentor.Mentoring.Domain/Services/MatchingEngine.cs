using GuidedMentor.Mentoring.Domain.ValueObjects;

namespace GuidedMentor.Mentoring.Domain.Services;

/// <summary>
/// Pure-function matching engine that computes compatibility scores between mentees and mentors.
/// All methods are deterministic with no side effects or external dependencies.
/// </summary>
public static class MatchingEngine
{
    /// <summary>Default page size for browse results.</summary>
    public const int DefaultPageSize = 12;

    /// <summary>
    /// Computes the full compatibility score between a mentee and mentor profile.
    /// </summary>
    /// <param name="mentee">The mentee profile to score against.</param>
    /// <param name="mentor">The mentor profile to score against.</param>
    /// <returns>A CompatibilityScore value object with all dimension scores.</returns>
    public static CompatibilityScore Compute(MenteeProfile mentee, MentorProfile mentor)
    {
        var chapterScore = ComputeChapterScore(mentee, mentor);
        var skillsScore = ComputeSkillsScore(mentee, mentor);
        var goalScore = ComputeGoalScore(mentee, mentor);
        var experienceScore = ComputeExperienceScore(mentee, mentor);

        return CompatibilityScore.Create(chapterScore, skillsScore, goalScore, experienceScore);
    }

    /// <summary>
    /// Computes the chapter dimension score (0-30).
    /// +30 same chapter, +15 same city different chapter, +0 otherwise.
    /// </summary>
    public static int ComputeChapterScore(MenteeProfile mentee, MentorProfile mentor)
    {
        if (mentee.Chapter == mentor.Chapter)
            return 30;

        if (ChapterCityMap.AreSameCity(mentee.Chapter, mentee.City, mentor.Chapter, mentor.City))
            return 15;

        return 0;
    }

    /// <summary>
    /// Computes the skills overlap dimension score (0-30).
    /// Formula: round((overlap / menteeSkillsCount) × 30).
    /// Returns 0 if the mentee has no skills.
    /// </summary>
    public static int ComputeSkillsScore(MenteeProfile mentee, MentorProfile mentor)
    {
        if (mentee.Skills.Count == 0)
            return 0;

        var overlap = mentee.Skills
            .Count(skill => mentor.ExpertiseAreas
                .Any(area => string.Equals(skill, area, StringComparison.OrdinalIgnoreCase)));

        return (int)Math.Round((double)overlap / mentee.Skills.Count * 30);
    }

    /// <summary>
    /// Computes the goal-topic alignment dimension score (0-25).
    /// Maps the mentee goal to topic categories, then checks how many mentor topics
    /// match those categories (case-insensitive).
    /// Formula: round((matchingTopics / relatedTopics.Count) × 25).
    /// Returns 0 if there are no related topics for the goal.
    /// </summary>
    public static int ComputeGoalScore(MenteeProfile mentee, MentorProfile mentor)
    {
        var relatedTopics = GoalTopicMap.GetTopicsForGoal(mentee.PrimaryGoal);

        if (relatedTopics.Count == 0)
            return 0;

        var matchingTopics = relatedTopics
            .Count(topic => mentor.Topics
                .Any(mentorTopic => string.Equals(mentorTopic, topic, StringComparison.OrdinalIgnoreCase)));

        return (int)Math.Round((double)matchingTopics / relatedTopics.Count * 25);
    }

    /// <summary>
    /// Computes the experience gap dimension score (0-15).
    /// Gap = mentor years - mentee years.
    /// ≥2 → 15, 1 → 10, 0 → 5, negative → 0.
    /// </summary>
    public static int ComputeExperienceScore(MenteeProfile mentee, MentorProfile mentor)
    {
        var gap = mentor.YearsOfExperience - mentee.YearsOfExperience;

        return gap switch
        {
            >= 2 => 15,
            1 => 10,
            0 => 5,
            _ => 0
        };
    }

    /// <summary>
    /// Gets paginated browse results: computes scores, excludes full-capacity mentors,
    /// sorts descending by score with alphabetical tiebreak by display name, then paginates.
    /// </summary>
    /// <param name="mentee">The mentee to compute scores for.</param>
    /// <param name="mentors">All mentor profiles to consider.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (default 12).</param>
    /// <returns>A paginated result of mentor-score pairs.</returns>
    public static PagedResult<MentorScoreResult> GetBrowseResults(
        MenteeProfile mentee,
        IReadOnlyList<MentorProfile> mentors,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;

        // Filter out full-capacity mentors and unavailable mentors
        var available = mentors
            .Where(m => m.ActiveMenteeCount < m.MaxMentees)
            .Where(m => m.AvailabilityStatus == AvailabilityStatus.Available);

        // Compute scores and sort
        var scored = available
            .Select(mentor => new MentorScoreResult(mentor, Compute(mentee, mentor)))
            .OrderByDescending(r => r.Score.Total)
            .ThenBy(r => r.Mentor.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalCount = scored.Count;
        var items = scored
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<MentorScoreResult>(items, totalCount, page, pageSize);
    }
}

/// <summary>
/// Represents a mentor paired with their computed compatibility score for a specific mentee.
/// </summary>
public sealed record MentorScoreResult(MentorProfile Mentor, CompatibilityScore Score);
