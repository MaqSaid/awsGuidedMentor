using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Services;

/// <summary>
/// Value object representing a multi-dimensional compatibility score between a mentee and a mentor.
/// Total score ranges from 0-100, composed of four dimensions.
/// </summary>
public sealed class CompatibilityScore : ValueObject
{
    /// <summary>Total compatibility score (0-100), sum of all dimensions.</summary>
    public int Total { get; }

    /// <summary>Chapter proximity score (0-30). +30 same chapter, +15 same city, +0 otherwise.</summary>
    public int ChapterScore { get; }

    /// <summary>Skills overlap score (0-30). Based on ratio of shared skills to mentee skills.</summary>
    public int SkillsOverlap { get; }

    /// <summary>Goal-topic alignment score (0-25). Based on how well mentor topics match mentee goal.</summary>
    public int GoalAlignment { get; }

    /// <summary>Experience gap score (0-15). Rewards mentors with more experience than mentee.</summary>
    public int ExperienceGap { get; }

    private CompatibilityScore(int chapterScore, int skillsOverlap, int goalAlignment, int experienceGap)
    {
        ChapterScore = chapterScore;
        SkillsOverlap = skillsOverlap;
        GoalAlignment = goalAlignment;
        ExperienceGap = experienceGap;
        Total = chapterScore + skillsOverlap + goalAlignment + experienceGap;
    }

    /// <summary>
    /// Factory method to create a CompatibilityScore from pre-computed dimension scores.
    /// </summary>
    internal static CompatibilityScore Create(int chapterScore, int skillsOverlap, int goalAlignment, int experienceGap)
    {
        return new CompatibilityScore(chapterScore, skillsOverlap, goalAlignment, experienceGap);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ChapterScore;
        yield return SkillsOverlap;
        yield return GoalAlignment;
        yield return ExperienceGap;
    }
}
