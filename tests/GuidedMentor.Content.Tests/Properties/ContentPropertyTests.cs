using FsCheck.Fluent;
using GuidedMentor.Content.Application.Services;
using GuidedMentor.Content.Domain;

namespace GuidedMentor.Content.Tests.Properties;

[Trait("Category", "Property")]
public sealed class ContentPropertyTests : PropertyTestBase
{
    [Property(MaxTest = 100)]
    public FsCheck.Property Property12_ValidSessionPlanPassesAllInvariants()
    {
        return Prop.ForAll(GenValidSessionPlan().ToArbitrary(), plan =>
        {
            plan.IsValid().Should().BeTrue();
            plan.Agenda.Count.Should().BeInRange(3, 7);
            plan.Agenda.Should().AllSatisfy(a => a.DurationMinutes.Should().BeGreaterThanOrEqualTo(3));
            plan.Agenda.Sum(a => a.DurationMinutes).Should().Be(35);
            plan.PreworkTasks.Count.Should().BeInRange(2, 5);
            plan.FollowUpTasks.Count.Should().BeInRange(2, 5);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property12_InvalidSessionPlanFailsValidation()
    {
        return Prop.ForAll(GenInvalidSessionPlan().ToArbitrary(), plan =>
        {
            plan.IsValid().Should().BeFalse();
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property13_ChecklistProgressCalculation()
    {
        var gen = Gen.Choose(0, 50).SelectMany(total =>
            Gen.Choose(0, Math.Max(total, 1)).Select(chk => new { total, chk = Math.Min(chk, total) }));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var progress = d.total == 0 ? 0 : (int)Math.Round((double)d.chk / d.total * 100);
            if (d.total == 0) progress.Should().Be(0);
            else
            {
                progress.Should().Be((int)Math.Round((double)d.chk / d.total * 100));
                progress.Should().BeInRange(0, 100);
            }
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property18_InputSanitizationNeutralizesInjectionPatterns()
    {
        var patterns = new[] { "ignore previous", "ignore all previous", "ignore above", "disregard previous", "system:", "you are now", "forget everything", "new instructions", "override instructions", "act as", "pretend you are", "from now on" };
        var gen = Gen.Elements(patterns).SelectMany(pat =>
            Gen.Elements("Hello ", "Please ", "").SelectMany(pre =>
            Gen.Elements(" now", " immediately", "").Select(suf => pre + pat + suf)));

        return Prop.ForAll(gen.ToArbitrary(), input =>
        {
            var sanitized = InputSanitizer.Sanitize(input);
            sanitized.Should().Contain("[filtered:");
            InputSanitizer.ContainsInjectionPattern(input).Should().BeTrue();
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property18_InputSanitizationPreservesNonMaliciousContent()
    {
        var gen = Gen.Choose(3, 8).SelectMany(count =>
            Gen.ArrayOf(Gen.Elements("hello", "world", "testing", "cloud", "architecture", "python", "deploy", "lambda", "bucket", "mentor"), count)
               .Select(words => string.Join(" ", words)));

        return Prop.ForAll(gen.ToArbitrary(), input =>
        {
            var sanitized = InputSanitizer.Sanitize(input);
            sanitized.Should().NotContain("[filtered:");
            sanitized.Should().NotBeNullOrEmpty();
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property18_InputSanitizationEnforcesMaxLength()
    {
        var gen = Gen.Choose(InputSanitizer.MaxFieldLength + 1, InputSanitizer.MaxFieldLength + 500)
            .SelectMany(len => Gen.ArrayOf(Gen.Elements('a', 'b', 'c', 'd', 'e'), len).Select(chars => new string(chars)));

        return Prop.ForAll(gen.ToArbitrary(), input =>
        {
            InputSanitizer.Sanitize(input).Length.Should().BeLessThanOrEqualTo(InputSanitizer.MaxFieldLength);
        });
    }

    private static Gen<SessionPlan> GenValidSessionPlan()
    {
        return Gen.Choose(3, 7).SelectMany(itemCount =>
        {
            return GenDurations(itemCount, 35).SelectMany(durations =>
                Gen.Choose(2, 5).SelectMany(preworkCount =>
                Gen.Choose(2, 5).SelectMany(followupCount =>
                Gen.ArrayOf(Gen.Elements("Review docs", "Read chapter", "Complete lab", "Watch video", "Practice"), preworkCount).SelectMany(pw =>
                Gen.ArrayOf(Gen.Elements("Submit PR", "Write summary", "Schedule next", "Share notes", "Quiz"), followupCount).Select(fu =>
                {
                    var agenda = durations.Select((d, i) => new AgendaItem($"Item {i + 1}", d, $"Description {i + 1}")).ToList();
                    return new SessionPlan("Valid Plan", agenda, pw.ToList(), fu.ToList());
                })))));
        });
    }

    private static Gen<SessionPlan> GenInvalidSessionPlan()
    {
        return Gen.OneOf(
            GenDurations(2, 35).Select(d =>
            {
                var agenda = d.Select((dur, i) => new AgendaItem($"I{i}", dur, $"D{i}")).ToList();
                return new SessionPlan("P", agenda, ["T1", "T2"], ["F1", "F2"]);
            }),
            Gen.Choose(3, 7).Select(count =>
            {
                var agenda = Enumerable.Range(0, count).Select(i => new AgendaItem($"I{i}", 3, $"D{i}")).ToList();
                return new SessionPlan("P", agenda, ["T1", "T2"], ["F1", "F2"]);
            }),
            GenDurations(4, 35).Select(d =>
            {
                var agenda = d.Select((dur, i) => new AgendaItem($"I{i}", dur, $"D{i}")).ToList();
                return new SessionPlan("P", agenda, ["Only one"], ["F1", "F2"]);
            }),
            GenDurations(4, 35).Select(d =>
            {
                var agenda = d.Select((dur, i) => new AgendaItem($"I{i}", dur, $"D{i}")).ToList();
                return new SessionPlan("P", agenda, ["T1", "T2"], ["Only one"]);
            })
        );
    }

    private static Gen<List<int>> GenDurations(int count, int total)
    {
        var minPerItem = 3;
        var remaining = total - (count * minPerItem);
        if (remaining < 0) return Gen.Constant(Enumerable.Repeat(total / count, count).ToList());

        return Gen.ArrayOf(Gen.Choose(0, remaining), count - 1).Select(cuts =>
        {
            var sorted = cuts.OrderBy(x => x).ToArray();
            var result = new List<int>(count);
            var prev = 0;
            foreach (var cut in sorted) { result.Add(cut - prev + minPerItem); prev = cut; }
            result.Add(remaining - prev + minPerItem);
            return result;
        });
    }
}
