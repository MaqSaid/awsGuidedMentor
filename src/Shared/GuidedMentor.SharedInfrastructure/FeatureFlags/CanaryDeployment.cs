namespace GuidedMentor.SharedInfrastructure.FeatureFlags;

/// <summary>
/// Represents the canary deployment stages for progressive feature rollouts.
/// Each stage defines the percentage of traffic receiving the new feature.
/// Progression: 1% → 10% → 50% → 100%.
/// </summary>
public static class CanaryDeployment
{
    /// <summary>Initial canary: 1% of traffic.</summary>
    public const int Stage1Percentage = 1;

    /// <summary>Early rollout: 10% of traffic.</summary>
    public const int Stage2Percentage = 10;

    /// <summary>Broad rollout: 50% of traffic.</summary>
    public const int Stage3Percentage = 50;

    /// <summary>Full rollout: 100% of traffic.</summary>
    public const int FullRolloutPercentage = 100;

    /// <summary>
    /// All canary stages in progression order.
    /// </summary>
    public static readonly int[] Stages = [Stage1Percentage, Stage2Percentage, Stage3Percentage, FullRolloutPercentage];

    /// <summary>
    /// Determines whether a given request should receive the feature
    /// based on a hash of the user identifier and the current rollout percentage.
    /// Uses consistent hashing so the same user always gets the same result for a given percentage.
    /// </summary>
    /// <param name="userId">The user identifier to hash.</param>
    /// <param name="rolloutPercentage">The current rollout percentage (0-100).</param>
    /// <returns>True if the user should receive the feature.</returns>
    public static bool IsUserInRollout(string userId, int rolloutPercentage)
    {
        if (rolloutPercentage >= FullRolloutPercentage)
            return true;

        if (rolloutPercentage <= 0)
            return false;

        // Consistent hash: same user always maps to same bucket
        var hash = (uint)userId.GetHashCode(StringComparison.Ordinal);
        var bucket = hash % 100;

        return bucket < (uint)rolloutPercentage;
    }
}
