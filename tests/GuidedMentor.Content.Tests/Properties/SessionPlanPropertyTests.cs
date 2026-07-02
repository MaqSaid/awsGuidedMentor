using FsCheck.Fluent;
using GuidedMentor.Content.Domain;

namespace GuidedMentor.Content.Tests.Properties;

/// <summary>
/// Property 12: Session Plan Structural Validity.
/// Validates that generated SessionPlans conform to all invariants when valid
/// and fail validation when any invariant is violated.
///
/// Property 13: Checklist Progress Calculation.
/// Validates progress = round((total_checked / total_items) × 100), 0 when total is 0.
/// </summary>
[Trait("Category", "Property")]
public sealed class SessionPlanPropertyTests : PropertyTestBase
{
    // =========================================================================
    // Property 12: Session Plan Structural Validity
    // =========================================================================

    [Property(MaxTest = 100)]
    public FsCheck.Property Property12_ValidSessionPlan_IsValidReturnsTrue()
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
    public FsCheck.Property Property12_InvalidSessionPlan_WrongItemCount_IsValidReturnsFalse()
    {
        var gen = Gen.OneOf(
            Gen.Choose(1, 2),
            Gen.Choose(8, 12))
            .SelectMany(count => GenDurations(count, 35).Select(durations =>
            {
                var agenda = durations.Select((d, i) => new AgendaItem($"Item {i + 1}", d, $"Desc {i}")).ToList();
                return new SessionPlan("Plan", agenda, ["T1", "T2"], ["F1", "F2"]);
            }));

        return Prop.ForAll(gen.ToArbitrary(), plan =>
        {
            plan.IsValid().Should().BeFalse("agenda item count is outside 3-7 range");
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property12_InvalidSessionPlan_WrongSum_IsValidReturnsFalse()
    {
        var gen = Gen.OneOf(
            Gen.Choose(20, 34),
            Gen.Choose(36, 60))
            .SelectMany(total => Gen.Choose(3, 7).SelectMany(count =>
                GenDurations(count, total).Select(durations =>
                {
                    var agenda = durations.Select((d, i) => new AgendaItem($"Item {i}", d, $"D{i}")).ToList();
                    return new SessionPlan("Plan", agenda, ["T1", "T2"], ["F1", "F2"]);
                })));

        return Prop.ForAll(gen.ToArbitrary(), plan =>
        {
            plan.IsValid().Should().BeFalse("agenda sum does not equal 35");
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property12_InvalidSessionPlan_ItemsUnder3Min_IsValidReturnsFalse()
    {
        var gen = Gen.Choose(4, 6).Select(count =>
        {
            var durations = new List<int> { 1 }; // First item is <3 min
            var remaining = 35 - 1;
            for (var i = 1; i < count; i++)
            {
                var dur = i < count - 1 ? Math.Min(remaining / (count - i), 10) : remaining;
                durations.Add(dur);
                remaining -= dur;
            }
            var agenda = durations.Select((d, i) => new AgendaItem($"Item {i}", d, $"D{i}")).ToList();
            return new SessionPlan("Plan", agenda, ["T1", "T2"], ["F1", "F2"]);
        });

        return Prop.ForAll(gen.ToArbitrary(), plan =>
        {
            plan.IsValid().Should().BeFalse("at least one agenda item has duration < 3 minutes");
        });
    }

    // =========================================================================
    // Property 13: Checklist Progress Calculation
    // =========================================================================

    [Property(MaxTest = 100)]
    public FsCheck.Property Property13_ChecklistProgress_NonZeroTotal_CalculatesCorrectly()
    {
        var gen = Gen.Choose(1, 50).SelectMany(total =>
            Gen.Choose(0, total).Select(checkedCount => new { total, checkedCount }));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var progress = CalculateProgress(d.checkedCount, d.total);
            progress.Should().BeInRange(0, 100);
            progress.Should().Be((int)Math.Round((double)d.checkedCount / d.total * 100));
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property13_ChecklistProgress_ZeroTotal_ReturnsZero()
    {
        return Prop.ForAll(Gen.Constant(0).ToArbitrary(), _ =>
        {
            var progress = CalculateProgress(0, 0);
            progress.Should().Be(0);
        });
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static int CalculateProgress(int checkedCount, int totalItems)
    {
        if (totalItems == 0) return 0;
        return (int)Math.Round((double)checkedCount / totalItems * 100);
    }

    private static Gen<SessionPlan> GenValidSessionPlan()
    {
        return Gen.Choose(3, 7).SelectMany(itemCount =>
            GenDurations(itemCount, 35).SelectMany(durations =>
            Gen.Choose(2, 5).SelectMany(preworkCount =>
            Gen.Choose(2, 5).SelectMany(followupCount =>
            Gen.ArrayOf(Gen.Elements("Review docs", "Read chapter", "Complete lab", "Watch video", "Practice"), preworkCount).SelectMany(pw =>
            Gen.ArrayOf(Gen.Elements("Submit PR", "Write summary", "Schedule next", "Share notes", "Quiz"), followupCount).Select(fu =>
            {
                var agenda = durations.Select((d, i) => new AgendaItem($"Item {i + 1}", d, $"Description {i + 1}")).ToList();
                return new SessionPlan("Valid Session Plan", agenda, pw.ToList(), fu.ToList());
            }))))));
    }

    private static Gen<List<int>> GenDurations(int count, int total)
    {
        var minPerItem = 3;
        var remaining = total - (count * minPerItem);
        if (remaining < 0)
            return Gen.Constant(Enumerable.Repeat(total / Math.Max(count, 1), count).ToList());

        return Gen.ArrayOf(Gen.Choose(0, remaining), count - 1).Select(cuts =>
        {
            var sorted = cuts.OrderBy(x => x).ToArray();
            var result = new List<int>(count);
            var prev = 0;
            foreach (var cut in sorted)
            {
                result.Add(cut - prev + minPerItem);
                prev = cut;
            }
            result.Add(remaining - prev + minPerItem);
            return result;
        });
    }
}
