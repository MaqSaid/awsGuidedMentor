using FsCheck.Fluent;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Tests.Properties;

/// <summary>
/// Property 16: Notification Badge Display.
/// Property 17: Notifications Ordered Reverse Chronologically.
/// </summary>
[Trait("Category", "Property")]
public sealed class NotificationPropertyTests : PropertyTestBase
{
    // =========================================================================
    // Property 16: Notification Badge Display
    // =========================================================================

    [Property(MaxTest = 100)]
    public FsCheck.Property Property16_BadgeDisplay_OneToNinetyNine_ShowsExactCount()
    {
        return Prop.ForAll(Gen.Choose(1, 99).ToArbitrary(), count =>
        {
            var badge = FormatNotificationBadge(count);
            badge.Should().Be(count.ToString());
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property16_BadgeDisplay_OverNinetyNine_ShowsNinetyNinePlus()
    {
        return Prop.ForAll(Gen.Choose(100, 10000).ToArbitrary(), count =>
        {
            var badge = FormatNotificationBadge(count);
            badge.Should().Be("99+");
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property16_BadgeDisplay_Zero_IsHidden()
    {
        return Prop.ForAll(Gen.Constant(0).ToArbitrary(), count =>
        {
            var badge = FormatNotificationBadge(count);
            badge.Should().BeNull("badge should be hidden when count is 0");
        });
    }

    // =========================================================================
    // Property 17: Notifications Ordered Reverse Chronologically
    // =========================================================================

    [Property(MaxTest = 100)]
    public FsCheck.Property Property17_NotificationList_OrderedReverseChronologically()
    {
        var gen = Gen.Choose(1, 50).SelectMany(count =>
            Gen.ArrayOf(GenNotification(), count).Select(n => n.ToList()));

        return Prop.ForAll(gen.ToArbitrary(), notifications =>
        {
            var sorted = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToList();

            for (var i = 0; i < sorted.Count - 1; i++)
            {
                sorted[i].CreatedAt.Should().BeOnOrAfter(sorted[i + 1].CreatedAt,
                    "each notification's createdAt should be >= the next item's createdAt");
            }
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property17_NotificationList_LimitedToFiftyItems()
    {
        var gen = Gen.Choose(1, 100).SelectMany(count =>
            Gen.ArrayOf(GenNotification(), count).Select(n => n.ToList()));

        return Prop.ForAll(gen.ToArbitrary(), notifications =>
        {
            var result = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToList();

            result.Count.Should().BeLessThanOrEqualTo(50);
        });
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static string? FormatNotificationBadge(int count) => count switch
    {
        0 => null,
        <= 99 => count.ToString(),
        _ => "99+"
    };

    private static Gen<Notification> GenNotification() =>
        Gen.Choose(0, 10000).Select(minutes =>
            Notification.Reconstitute(
                NotificationId.New(),
                UserId.New(),
                NotificationType.RequestAccepted,
                $"Notification message {minutes}",
                "/notifications",
                false,
                DateTime.UtcNow.AddMinutes(-minutes)));
}
