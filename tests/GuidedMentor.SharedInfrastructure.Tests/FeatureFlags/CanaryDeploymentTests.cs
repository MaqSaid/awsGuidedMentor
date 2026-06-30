using GuidedMentor.SharedInfrastructure.FeatureFlags;

namespace GuidedMentor.SharedInfrastructure.Tests.FeatureFlags;

public sealed class CanaryDeploymentTests
{
    [Fact]
    public void IsUserInRollout_AtFullRollout_AlwaysReturnsTrue()
    {
        // Any user should be included at 100%
        Assert.True(CanaryDeployment.IsUserInRollout("user-123", 100));
        Assert.True(CanaryDeployment.IsUserInRollout("user-456", 100));
        Assert.True(CanaryDeployment.IsUserInRollout(Guid.NewGuid().ToString(), 100));
    }

    [Fact]
    public void IsUserInRollout_AtZeroPercent_AlwaysReturnsFalse()
    {
        Assert.False(CanaryDeployment.IsUserInRollout("user-123", 0));
        Assert.False(CanaryDeployment.IsUserInRollout("user-456", 0));
        Assert.False(CanaryDeployment.IsUserInRollout(Guid.NewGuid().ToString(), 0));
    }

    [Fact]
    public void IsUserInRollout_IsConsistent_SameUserSameResult()
    {
        // Same user + same percentage should always give same result
        var userId = "consistent-user-abc";
        var firstResult = CanaryDeployment.IsUserInRollout(userId, 50);
        var secondResult = CanaryDeployment.IsUserInRollout(userId, 50);
        var thirdResult = CanaryDeployment.IsUserInRollout(userId, 50);

        Assert.Equal(firstResult, secondResult);
        Assert.Equal(secondResult, thirdResult);
    }

    [Fact]
    public void IsUserInRollout_HigherPercentage_IncludesMoreUsers()
    {
        // Generate a set of users and count how many are included at different percentages
        var users = Enumerable.Range(0, 1000)
            .Select(i => $"user-{i}")
            .ToList();

        var countAt1 = users.Count(u => CanaryDeployment.IsUserInRollout(u, 1));
        var countAt10 = users.Count(u => CanaryDeployment.IsUserInRollout(u, 10));
        var countAt50 = users.Count(u => CanaryDeployment.IsUserInRollout(u, 50));
        var countAt100 = users.Count(u => CanaryDeployment.IsUserInRollout(u, 100));

        // Monotonically increasing (or equal)
        Assert.True(countAt1 <= countAt10);
        Assert.True(countAt10 <= countAt50);
        Assert.True(countAt50 <= countAt100);
        Assert.Equal(1000, countAt100);
    }

    [Fact]
    public void Stages_ContainsExpectedProgressionValues()
    {
        Assert.Equal([1, 10, 50, 100], CanaryDeployment.Stages);
    }

    [Fact]
    public void IsUserInRollout_NegativePercentage_ReturnsFalse()
    {
        Assert.False(CanaryDeployment.IsUserInRollout("any-user", -5));
    }

    [Fact]
    public void IsUserInRollout_OverOneHundredPercent_ReturnsTrue()
    {
        Assert.True(CanaryDeployment.IsUserInRollout("any-user", 150));
    }
}
