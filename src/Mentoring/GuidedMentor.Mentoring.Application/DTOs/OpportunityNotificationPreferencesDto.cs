using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.ValueObjects;

namespace GuidedMentor.Mentoring.Application.DTOs;

/// <summary>
/// DTO for mentee opportunity notification preferences.
/// </summary>
public sealed record OpportunityNotificationPreferencesDto(
    bool IsEnabled,
    IReadOnlyList<OpportunityType> TypePreferences,
    bool SkillMatchEnabled)
{
    public static OpportunityNotificationPreferencesDto FromDomain(OpportunityNotificationPreferences preferences)
    {
        return new OpportunityNotificationPreferencesDto(
            preferences.IsEnabled,
            preferences.TypePreferences,
            preferences.SkillMatchEnabled);
    }
}
