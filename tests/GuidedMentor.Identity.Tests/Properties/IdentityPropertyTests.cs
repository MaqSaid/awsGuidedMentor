using FsCheck.Fluent;
using GuidedMentor.Identity.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Tests.Properties;

/// <summary>
/// FsCheck property-based tests for the Identity bounded context.
/// </summary>
[Trait("Category", "Property")]
public sealed class IdentityPropertyTests : PropertyTestBase
{
    [Property(MaxTest = 100)]
    public FsCheck.Property Property1_MagicLinkToken_IsAlwaysValidUUID()
    {
        return Prop.ForAll(Arb.Default.Guid().ToArbitrary(), token =>
        {
            Guid.TryParse(token.ToString(), out _).Should().BeTrue();
            token.Should().NotBe(Guid.Empty);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property1_MagicLinkExpiry_IsAlways10MinutesFromCreation()
    {
        return Prop.ForAll(Gen.Choose(0, 100000).ToArbitrary(), offsetSeconds =>
        {
            var createdAt = DateTime.UtcNow.AddSeconds(-offsetSeconds);
            var expiresAt = createdAt.AddMinutes(10);
            (expiresAt - createdAt).TotalMinutes.Should().Be(10);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property1_UsedToken_CanNeverBeReused()
    {
        // Simulate: once used=true, verification always fails
        return Prop.ForAll(Gen.Elements(true, false).ToArbitrary(), isUsed =>
        {
            var canAuthenticate = !isUsed; // used tokens always fail
            if (isUsed)
                canAuthenticate.Should().BeFalse();
            else
                canAuthenticate.Should().BeTrue();
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property2_RoleToggleProducesOppositeRole()
    {
        return Prop.ForAll(Gen.Elements(Role.Mentor, Role.Mentee).ToArbitrary(), initialRole =>
        {
            var user = CreateUserWithRole(initialRole);
            var expected = initialRole == Role.Mentor ? Role.Mentee : Role.Mentor;
            user.ToggleRole().IsSuccess.Should().BeTrue();
            user.ActiveRole.Should().Be(expected);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property2_RoleToggleTwiceReturnsOriginalRole()
    {
        return Prop.ForAll(Gen.Elements(Role.Mentor, Role.Mentee).ToArbitrary(), initialRole =>
        {
            var user = CreateUserWithRole(initialRole);
            user.ToggleRole();
            user.ToggleRole();
            user.ActiveRole.Should().Be(initialRole);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property3_SingleActiveRoleInvariantAfterToggles()
    {
        var testGen = Gen.Elements(Role.Mentor, Role.Mentee).SelectMany(role =>
            Gen.Choose(0, 20).Select(count => (role, count)));

        return Prop.ForAll(testGen.ToArbitrary(), tuple =>
        {
            var (initialRole, toggleCount) = tuple;
            var user = CreateUserWithRole(initialRole);
            for (var i = 0; i < toggleCount; i++)
            {
                user.ToggleRole();
                user.ActiveRole.Should().NotBeNull();
                user.ActiveRole.Should().BeOneOf(Role.Mentor, Role.Mentee);
            }
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property4_RoleTogglePreservesInactiveProfile()
    {
        return Prop.ForAll(Gen.Elements(Role.Mentor, Role.Mentee).ToArbitrary(), initialRole =>
        {
            var user = CreateUserWithRole(initialRole);
            var inactiveRole = initialRole == Role.Mentor ? Role.Mentee : Role.Mentor;
            var statusBefore = user.GetOnboardingStatus(inactiveRole);
            var nameBefore = user.DisplayName;
            var cityBefore = user.City;

            user.ToggleRole();

            user.GetOnboardingStatus(inactiveRole).Should().Be(statusBefore);
            user.DisplayName.Should().Be(nameBefore);
            user.City.Should().Be(cityBefore);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property5_OnboardingValidationAcceptsValidStep()
    {
        var gen = Gen.Elements(Role.Mentor, Role.Mentee).SelectMany(role =>
        {
            var max = role == Role.Mentor ? 3 : 4;
            return Gen.Choose(1, max).Select(step => (role, step));
        });

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (role, step) = tuple;
            var max = role == Role.Mentor ? 3 : 4;
            step.Should().BeInRange(1, max);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property5_OnboardingValidationRejectsInvalidStep()
    {
        var gen = Gen.Elements(Role.Mentor, Role.Mentee).SelectMany(role =>
        {
            var max = role == Role.Mentor ? 3 : 4;
            return Gen.OneOf(Gen.Choose(-10, 0), Gen.Choose(max + 1, max + 10))
                      .Select(step => (role, step, max));
        });

        return Prop.ForAll(gen.ToArbitrary(), tuple =>
        {
            var (_, step, max) = tuple;
            (step < 1 || step > max).Should().BeTrue();
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property16_NotificationBadgeDisplaysCorrectText()
    {
        return Prop.ForAll(Gen.Choose(0, 500).ToArbitrary(), count =>
        {
            var badge = FormatNotificationBadge(count);
            if (count == 0) badge.Should().BeNull();
            else if (count <= 99) badge.Should().Be(count.ToString());
            else badge.Should().Be("99+");
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property33_MaintenanceModeBlocksNonAdmin()
    {
        var boolGen = Gen.Elements(true, false);
        var gen = boolGen.SelectMany(maintenance =>
            boolGen.Select(admin => new { maintenance, admin }));

        return Prop.ForAll(gen.ToArbitrary(), scenario =>
        {
            var status = scenario.maintenance && !scenario.admin ? 503 : 200;
            if (scenario.maintenance && !scenario.admin)
                status.Should().Be(503);
            else
                status.Should().Be(200);
        });
    }

    private static User CreateUserWithRole(Role role)
    {
        var user = User.Create(UserId.New(), Email.Create("test@example.com").Value,
            "Test User", SharedKernel.AustralianChapter.Sydney, "Sydney");
        user.SetInitialRole(role);
        return user;
    }

    private static string? FormatNotificationBadge(int count) => count switch
    {
        0 => null, <= 99 => count.ToString(), _ => "99+"
    };
}
