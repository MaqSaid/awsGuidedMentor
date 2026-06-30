using GuidedMentor.Engagement.Application.Services;
using Microsoft.Extensions.Time.Testing;

namespace GuidedMentor.Engagement.Tests;

/// <summary>
/// Unit tests for the ChatRateLimiter.
/// Validates: Requirements 14.11
/// </summary>
public sealed class ChatRateLimiterTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly ChatRateLimiter _rateLimiter;

    public ChatRateLimiterTests()
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _rateLimiter = new ChatRateLimiter(_timeProvider);
    }

    [Fact]
    public void IsAllowed_FirstMessage_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _rateLimiter.IsAllowed(userId).Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_Under20Messages_AllAllowed()
    {
        var userId = Guid.NewGuid();

        for (var i = 0; i < 20; i++)
        {
            _rateLimiter.IsAllowed(userId).Should().BeTrue($"Message {i + 1} should be allowed");
        }
    }

    [Fact]
    public void IsAllowed_21stMessage_ReturnsFalse()
    {
        var userId = Guid.NewGuid();

        for (var i = 0; i < 20; i++)
        {
            _rateLimiter.IsAllowed(userId);
        }

        _rateLimiter.IsAllowed(userId).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_AfterWindowExpires_AllowsAgain()
    {
        var userId = Guid.NewGuid();

        // Exhaust the limit
        for (var i = 0; i < 20; i++)
        {
            _rateLimiter.IsAllowed(userId);
        }

        _rateLimiter.IsAllowed(userId).Should().BeFalse();

        // Advance time past the 1-minute window
        _timeProvider.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromMilliseconds(1)));

        _rateLimiter.IsAllowed(userId).Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_DifferentUsers_IndependentLimits()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        // Exhaust user1's limit
        for (var i = 0; i < 20; i++)
        {
            _rateLimiter.IsAllowed(user1);
        }

        // user2 should still be allowed
        _rateLimiter.IsAllowed(user2).Should().BeTrue();
        _rateLimiter.IsAllowed(user1).Should().BeFalse();
    }

    [Fact]
    public void GetRemainingMessages_NewUser_Returns20()
    {
        var userId = Guid.NewGuid();
        _rateLimiter.GetRemainingMessages(userId).Should().Be(20);
    }

    [Fact]
    public void GetRemainingMessages_After5Messages_Returns15()
    {
        var userId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
        {
            _rateLimiter.IsAllowed(userId);
        }

        _rateLimiter.GetRemainingMessages(userId).Should().Be(15);
    }

    [Fact]
    public void GetRemainingMessages_AfterLimitExhausted_Returns0()
    {
        var userId = Guid.NewGuid();

        for (var i = 0; i < 20; i++)
        {
            _rateLimiter.IsAllowed(userId);
        }

        _rateLimiter.GetRemainingMessages(userId).Should().Be(0);
    }

    [Fact]
    public void SlidingWindow_OldMessagesExpire_AllowsNew()
    {
        var userId = Guid.NewGuid();

        // Send 10 messages at t=0
        for (var i = 0; i < 10; i++)
        {
            _rateLimiter.IsAllowed(userId);
        }

        // Advance 30 seconds, send 10 more
        _timeProvider.Advance(TimeSpan.FromSeconds(30));

        for (var i = 0; i < 10; i++)
        {
            _rateLimiter.IsAllowed(userId);
        }

        // Now at limit (20 messages in 30s window)
        _rateLimiter.IsAllowed(userId).Should().BeFalse();

        // Advance 31 more seconds — the first 10 messages expire (they were 61s ago)
        _timeProvider.Advance(TimeSpan.FromSeconds(31));

        // Should allow up to 10 more messages (the second batch is still within window)
        _rateLimiter.IsAllowed(userId).Should().BeTrue();
        _rateLimiter.GetRemainingMessages(userId).Should().Be(9);
    }

    [Fact]
    public void MaxMessagesPerWindow_Is20()
    {
        ChatRateLimiter.MaxMessagesPerWindow.Should().Be(20);
    }

    [Fact]
    public void WindowDuration_IsOneMinute()
    {
        ChatRateLimiter.WindowDuration.Should().Be(TimeSpan.FromMinutes(1));
    }
}
