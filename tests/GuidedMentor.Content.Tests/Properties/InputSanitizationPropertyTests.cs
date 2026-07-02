using FsCheck.Fluent;
using GuidedMentor.Content.Application.Services;

namespace GuidedMentor.Content.Tests.Properties;

/// <summary>
/// Property 18: Input Sanitization Prevents Prompt Injection.
/// Validates that malicious patterns are neutralized (wrapped in [filtered: ...]),
/// non-malicious strings are preserved unchanged, and all output ≤ 2000 chars.
/// </summary>
[Trait("Category", "Property")]
public sealed class InputSanitizationPropertyTests : PropertyTestBase
{
    private static readonly string[] InjectionPatterns =
    [
        "ignore previous", "ignore all previous", "ignore above",
        "disregard previous", "system:", "you are now",
        "forget everything", "new instructions", "override instructions",
        "act as", "pretend you are", "from now on"
    ];

    [Property(MaxTest = 100)]
    public FsCheck.Property Property18_MaliciousStrings_AreNeutralized()
    {
        var gen = Gen.Elements(InjectionPatterns).SelectMany(pattern =>
            Gen.Elements("Hello ", "Please ", "Can you ", "").SelectMany(prefix =>
            Gen.Elements(" now", " immediately", " please", "").Select(suffix =>
                prefix + pattern + suffix)));

        return Prop.ForAll(gen.ToArbitrary(), input =>
        {
            var sanitized = InputSanitizer.Sanitize(input);
            sanitized.Should().Contain("[filtered:");
            InputSanitizer.ContainsInjectionPattern(input).Should().BeTrue();
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property18_NonMaliciousStrings_PreservedUnchanged()
    {
        var safeWords = new[]
        {
            "hello", "world", "testing", "cloud", "architecture",
            "python", "deploy", "lambda", "bucket", "mentor",
            "session", "plan", "review", "code", "build"
        };

        var gen = Gen.Choose(3, 8).SelectMany(count =>
            Gen.ArrayOf(Gen.Elements(safeWords), count)
               .Select(words => string.Join(" ", words)));

        return Prop.ForAll(gen.ToArbitrary(), input =>
        {
            var sanitized = InputSanitizer.Sanitize(input);
            sanitized.Should().NotContain("[filtered:");
            sanitized.Should().NotBeNullOrEmpty();
            sanitized.Should().Be(input);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property18_AllOutput_DoesNotExceed2000Chars()
    {
        var gen = Gen.Choose(1, 3000).SelectMany(len =>
            Gen.ArrayOf(Gen.Elements('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j'), len)
               .Select(chars => new string(chars)));

        return Prop.ForAll(gen.ToArbitrary(), input =>
        {
            var sanitized = InputSanitizer.Sanitize(input);
            sanitized.Length.Should().BeLessThanOrEqualTo(InputSanitizer.MaxFieldLength);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property18_MaliciousWithLongPrefix_StillDetected()
    {
        var gen = Gen.Choose(50, 200).SelectMany(prefixLen =>
            Gen.ArrayOf(Gen.Elements('a', 'b', 'c', ' ', 'd', 'e'), prefixLen).SelectMany(prefix =>
            Gen.Elements(InjectionPatterns).Select(pattern =>
                new string(prefix) + " " + pattern)));

        return Prop.ForAll(gen.ToArbitrary(), input =>
        {
            var sanitized = InputSanitizer.Sanitize(input);
            sanitized.Should().Contain("[filtered:");
        });
    }
}
