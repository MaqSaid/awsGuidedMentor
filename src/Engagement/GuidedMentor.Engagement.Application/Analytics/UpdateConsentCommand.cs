namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Command to update a user's tracking consent preference.
///
/// Requirements: 30.7, 30.8
/// </summary>
public sealed record UpdateConsentCommand(
    Guid UserId,
    string Consent);
