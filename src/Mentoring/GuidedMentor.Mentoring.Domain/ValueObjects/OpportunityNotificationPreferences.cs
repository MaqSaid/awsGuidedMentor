using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.ValueObjects;

/// <summary>
/// Represents a mentee's notification preferences for opportunity postings.
/// Controls which types of opportunities trigger notifications and whether
/// skill-match notifications are enabled.
/// </summary>
public sealed class OpportunityNotificationPreferences : ValueObject
{
    /// <summary>Whether opportunity notifications are enabled overall.</summary>
    public bool IsEnabled { get; }

    /// <summary>Which opportunity types the mentee wants notifications for. Default: all types.</summary>
    public IReadOnlyList<OpportunityType> TypePreferences { get; }

    /// <summary>Whether skill-match notifications are enabled (≥2 skill overlap from any mentor).</summary>
    public bool SkillMatchEnabled { get; }

    private OpportunityNotificationPreferences(
        bool isEnabled,
        IReadOnlyList<OpportunityType> typePreferences,
        bool skillMatchEnabled)
    {
        IsEnabled = isEnabled;
        TypePreferences = typePreferences;
        SkillMatchEnabled = skillMatchEnabled;
    }

    /// <summary>
    /// Creates default preferences (all enabled, all types, skill-match on).
    /// </summary>
    public static OpportunityNotificationPreferences Default()
    {
        return new OpportunityNotificationPreferences(
            isEnabled: true,
            typePreferences: [OpportunityType.Job, OpportunityType.Workshop, OpportunityType.Event, OpportunityType.Training],
            skillMatchEnabled: true);
    }

    /// <summary>
    /// Creates custom preferences.
    /// </summary>
    public static OpportunityNotificationPreferences Create(
        bool isEnabled,
        IReadOnlyList<OpportunityType> typePreferences,
        bool skillMatchEnabled)
    {
        return new OpportunityNotificationPreferences(isEnabled, typePreferences, skillMatchEnabled);
    }

    /// <summary>
    /// Checks whether a notification should be sent for the given opportunity type.
    /// </summary>
    public bool ShouldNotifyForType(OpportunityType type)
    {
        return IsEnabled && TypePreferences.Contains(type);
    }

    /// <summary>
    /// Checks whether a skill-match notification should be sent.
    /// </summary>
    public bool ShouldNotifyForSkillMatch(OpportunityType type, IReadOnlyList<string> menteeSkills, IReadOnlyList<string> postingSkills)
    {
        if (!IsEnabled || !SkillMatchEnabled)
            return false;

        if (!TypePreferences.Contains(type))
            return false;

        var overlap = menteeSkills.Intersect(postingSkills, StringComparer.OrdinalIgnoreCase).Count();
        return overlap >= 2;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return SkillMatchEnabled;
        foreach (var type in TypePreferences.OrderBy(t => t))
        {
            yield return type;
        }
    }
}
