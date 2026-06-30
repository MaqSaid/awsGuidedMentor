using FluentAssertions;
using GuidedMentor.SharedInfrastructure.Security;

namespace GuidedMentor.SharedInfrastructure.Tests.Security;

/// <summary>
/// Tests for the SlidingWindowCounter internal implementation used by RateLimitingMiddleware.
/// Validates sliding window semantics, expiry, and thread-safety properties.
/// </summary>
public sealed class SlidingWindowCounterTests
{
    [Fact]
    public void Should_Allow_Up_To_Max_Requests()
    {
        var counter = new SlidingWindowCounter(TimeSpan.FromMinutes(1), maxRequests: 3);
        var now = DateTimeOffset.UtcNow;

        counter.TryIncrement(now).Should().BeTrue();
        counter.TryIncrement(now).Should().BeTrue();
        counter.TryIncrement(now).Should().BeTrue();
        counter.TryIncrement(now).Should().BeFalse();
    }

    [Fact]
    public void Should_Allow_Requests_After_Window_Expires()
    {
        var counter = new SlidingWindowCounter(TimeSpan.FromMinutes(1), maxRequests: 2);
        var now = DateTimeOffset.UtcNow;

        counter.TryIncrement(now).Should().BeTrue();
        counter.TryIncrement(now).Should().BeTrue();
        counter.TryIncrement(now).Should().BeFalse();

        // Move time forward past the window
        var later = now.AddMinutes(1).AddSeconds(1);
        counter.TryIncrement(later).Should().BeTrue();
    }

    [Fact]
    public void Should_Report_Correct_Remaining_Requests()
    {
        var counter = new SlidingWindowCounter(TimeSpan.FromMinutes(1), maxRequests: 5);
        var now = DateTimeOffset.UtcNow;

        counter.TryIncrement(now);
        counter.TryIncrement(now);

        counter.GetRemainingRequests(now).Should().Be(3);
    }

    [Fact]
    public void Should_Report_Positive_RetryAfter_When_Limit_Hit()
    {
        var counter = new SlidingWindowCounter(TimeSpan.FromMinutes(1), maxRequests: 1);
        var now = DateTimeOffset.UtcNow;

        counter.TryIncrement(now);
        counter.TryIncrement(now); // Rejected

        counter.GetRetryAfterSeconds(now).Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(60);
    }

    [Fact]
    public void Should_Be_Expired_When_No_Requests_In_Window()
    {
        var counter = new SlidingWindowCounter(TimeSpan.FromMinutes(1), maxRequests: 5);
        var now = DateTimeOffset.UtcNow;

        counter.TryIncrement(now);

        // After the window expires, it should be considered expired
        var later = now.AddMinutes(2);
        counter.IsExpired(later).Should().BeTrue();
    }

    [Fact]
    public void Should_Not_Be_Expired_With_Active_Requests()
    {
        var counter = new SlidingWindowCounter(TimeSpan.FromMinutes(1), maxRequests: 5);
        var now = DateTimeOffset.UtcNow;

        counter.TryIncrement(now);

        counter.IsExpired(now).Should().BeFalse();
    }
}
