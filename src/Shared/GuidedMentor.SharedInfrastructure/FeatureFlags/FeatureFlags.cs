namespace GuidedMentor.SharedInfrastructure.FeatureFlags;

/// <summary>
/// Well-known feature flag names used across the platform.
/// These correspond to AWS AppConfig feature flags for progressive rollouts.
/// </summary>
public static class FeatureFlags
{
    /// <summary>AI Help floating chat assistant (Bedrock Claude).</summary>
    public const string AiHelp = "AiHelp";

    /// <summary>Opportunities/Job Board feature for mentors to post opportunities.</summary>
    public const string JobBoard = "JobBoard";

    /// <summary>Meetup Calendar integration for session scheduling alignment.</summary>
    public const string MeetupCalendar = "MeetupCalendar";

    /// <summary>AI-generated session plans via Bedrock.</summary>
    public const string SessionPlans = "SessionPlans";
}
