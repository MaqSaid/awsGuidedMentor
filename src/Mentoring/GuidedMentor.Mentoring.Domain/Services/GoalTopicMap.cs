namespace GuidedMentor.Mentoring.Domain.Services;

/// <summary>
/// Maps mentee primary goals to related topic categories for goal-topic alignment scoring.
/// The matching algorithm uses this to determine how many of a mentor's topics
/// align with a mentee's primary goal.
/// </summary>
public static class GoalTopicMap
{
    private static readonly Dictionary<PrimaryGoal, IReadOnlyList<string>> GoalToTopics = new()
    {
        [PrimaryGoal.CareerTransition] = new[]
        {
            "career guidance",
            "interview prep",
            "resume review"
        },
        [PrimaryGoal.SkillDevelopment] = new[]
        {
            "hands-on labs",
            "code review",
            "architecture"
        },
        [PrimaryGoal.CertificationPreparation] = new[]
        {
            "certification study",
            "exam prep",
            "practice tests"
        },
        [PrimaryGoal.ProjectGuidance] = new[]
        {
            "project planning",
            "code review",
            "architecture"
        }
    };

    /// <summary>
    /// Gets the related topic categories for a given primary goal.
    /// </summary>
    public static IReadOnlyList<string> GetTopicsForGoal(PrimaryGoal goal)
    {
        return GoalToTopics.TryGetValue(goal, out var topics) ? topics : [];
    }

    /// <summary>
    /// Gets all unique topic categories across all goals.
    /// </summary>
    public static IReadOnlyList<string> GetAllTopicCategories()
    {
        return GoalToTopics.Values.SelectMany(t => t).Distinct().ToList();
    }
}
