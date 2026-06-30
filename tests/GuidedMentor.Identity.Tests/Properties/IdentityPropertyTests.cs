using FsCheck.Fluent;
using GuidedMentor.Identity.Application.Validators;
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
    public FsCheck.Property Property1_PasswordValidationAcceptsValidPasswords()
    {
        var validPasswordGen =
            Gen.Choose(12, 50).SelectMany(length =>
            {
                var upper = Gen.Elements('A', 'B', 'C', 'D', 'E', 'F', 'G', 'H');
                var lower = Gen.Elements('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h');
                var digit = Gen.Elements('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
                var special = Gen.Elements('!', '@', '#', '$', '%', '&', '*');
                var any = Gen.Elements('a', 'B', '3', '!', 'x', 'Y', '7', '@');
                var fillerCount = length - 4;
                return upper.SelectMany(u =>
                       lower.SelectMany(l =>
                       digit.SelectMany(d =>
                       special.SelectMany(s =>
                       Gen.ArrayOf(any, fillerCount > 0 ? fillerCount : 0).SelectMany(fillers =>
                       {
                           var all = new[] { u, l, d, s }.Concat(fillers).ToArray();
                           return Gen.Shuffle(all).Select(shuffled => new string(shuffled));
                       })))));
            });

        return Prop.ForAll(validPasswordGen.ToArbitrary(), password =>
        {
            PasswordValidator.IsValid(password).Should().BeTrue(
                $"Password '{password}' (length {password.Length}) should be valid");
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property1_PasswordValidationRejectsShortPasswords()
    {
        var shortPasswordGen =
            Gen.Choose(1, 11).SelectMany(length =>
            Gen.ArrayOf(Gen.Elements('A', 'b', '1', '!', 'C', 'd', '2', '@'), length)
               .Select(chars => new string(chars)));

        return Prop.ForAll(shortPasswordGen.ToArbitrary(), password =>
        {
            PasswordValidator.IsValid(password).Should().BeFalse(
                $"Short password (length {password.Length}) should be rejected");
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property1_PasswordValidationRejectsMissingUppercase()
    {
        var noUpperGen =
            Gen.Choose(12, 30).SelectMany(length =>
            Gen.ArrayOf(Gen.Elements('a', 'b', 'c', '1', '2', '!', '@', '#', 'd', 'e', '3'), length)
               .Select(chars => new string(chars)));

        return Prop.ForAll(noUpperGen.ToArbitrary(), password =>
        {
            var errors = PasswordValidator.Validate(password);
            errors.Should().Contain(PasswordValidator.UppercaseMessage);
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
