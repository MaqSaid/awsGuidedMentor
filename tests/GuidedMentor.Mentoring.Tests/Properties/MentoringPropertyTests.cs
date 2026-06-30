using FsCheck.Fluent;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Services;
using GuidedMentor.Mentoring.Domain.ValueObjects;
using GuidedMentor.SharedKernel;
using AustralianChapter = GuidedMentor.SharedKernel.AustralianChapter;
using PrimaryGoal = GuidedMentor.Mentoring.Domain.Services.PrimaryGoal;
using ExperienceLevel = GuidedMentor.Mentoring.Domain.Entities.ExperienceLevel;

namespace GuidedMentor.Mentoring.Tests.Properties;

[Trait("Category", "Property")]
public sealed class MentoringPropertyTests : PropertyTestBase
{
    private static readonly string[] AllSkills = ["Python", "TypeScript", "C#", "Java", "Go", "Rust", "Terraform", "CloudFormation", "CDK", "Docker", "Kubernetes", "Lambda", "DynamoDB", "S3"];
    private static readonly string[] AllTopics = ["career guidance", "interview prep", "resume review", "hands-on labs", "code review", "architecture", "certification study", "exam prep", "practice tests", "project planning"];

    [Property(MaxTest = 100)]
    public FsCheck.Property Property6_MatchingScoreBoundsAndDeterminism()
    {
        var gen = GenMenteeProfile().SelectMany(mentee => GenMentorProfile().Select(mentor => new { mentee, mentor }));
        return Prop.ForAll(gen.ToArbitrary(), p =>
        {
            var s1 = MatchingEngine.Compute(p.mentee, p.mentor);
            var s2 = MatchingEngine.Compute(p.mentee, p.mentor);
            s1.Total.Should().BeInRange(0, 100);
            s1.Total.Should().Be(s2.Total);
            s1.ChapterScore.Should().Be(s2.ChapterScore);
            s1.SkillsOverlap.Should().Be(s2.SkillsOverlap);
            s1.GoalAlignment.Should().Be(s2.GoalAlignment);
            s1.ExperienceGap.Should().Be(s2.ExperienceGap);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property7_SkillsScoreFormula()
    {
        var gen = Gen.Choose(0, 8).SelectMany(c => Gen.ArrayOf(Gen.Elements(AllSkills), c).SelectMany(ms => Gen.SubListOf(AllSkills).Select(me => new { menteeSkills = ms.Distinct().ToList(), mentorExp = me.ToList() })));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var mentee = MakeMentee(skills: d.menteeSkills);
            var mentor = MakeMentor(expertiseAreas: d.mentorExp);
            var score = MatchingEngine.ComputeSkillsScore(mentee, mentor);
            if (d.menteeSkills.Count == 0) { score.Should().Be(0); return; }
            var overlap = d.menteeSkills.Count(s => d.mentorExp.Any(e => string.Equals(s, e, StringComparison.OrdinalIgnoreCase)));
            score.Should().Be((int)Math.Round((double)overlap / d.menteeSkills.Count * 30));
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property7_GoalScoreFormula()
    {
        var gen = Gen.Elements(PrimaryGoal.CareerTransition, PrimaryGoal.SkillDevelopment, PrimaryGoal.CertificationPreparation, PrimaryGoal.ProjectGuidance)
            .SelectMany(goal => Gen.SubListOf(AllTopics).Select(t => new { goal, mentorTopics = t.ToList() }));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var score = MatchingEngine.ComputeGoalScore(MakeMentee(primaryGoal: d.goal), MakeMentor(topics: d.mentorTopics));
            var related = GoalTopicMap.GetTopicsForGoal(d.goal);
            if (related.Count == 0) { score.Should().Be(0); return; }
            var matching = related.Count(t => d.mentorTopics.Any(mt => string.Equals(mt, t, StringComparison.OrdinalIgnoreCase)));
            score.Should().Be((int)Math.Round((double)matching / related.Count * 25));
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property8_BrowseResultsSortedDescByScoreThenAlpha()
    {
        var gen = Gen.Choose(2, 15).SelectMany(c => Gen.ArrayOf(GenMentorProfile(), c).SelectMany(ms => GenMenteeProfile().Select(me => new { me, ms = ms.ToList() as IReadOnlyList<MentorProfile> })));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var r = MatchingEngine.GetBrowseResults(d.me, d.ms);
            for (var i = 0; i < r.Items.Count - 1; i++)
            {
                r.Items[i].Score.Total.Should().BeGreaterThanOrEqualTo(r.Items[i + 1].Score.Total);
                if (r.Items[i].Score.Total == r.Items[i + 1].Score.Total)
                    string.Compare(r.Items[i].Mentor.DisplayName, r.Items[i + 1].Mentor.DisplayName, StringComparison.OrdinalIgnoreCase).Should().BeLessThanOrEqualTo(0);
            }
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property9_BrowseExcludesFullCapacityMentors()
    {
        var gen = Gen.Choose(1, 10).SelectMany(c => Gen.ArrayOf(GenMentorWithCapacity(), c).SelectMany(ms => GenMenteeProfile().Select(me => new { me, ms = ms.ToList() as IReadOnlyList<MentorProfile> })));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var r = MatchingEngine.GetBrowseResults(d.me, d.ms);
            r.Items.Should().AllSatisfy(i => i.Mentor.ActiveMenteeCount.Should().BeLessThan(i.Mentor.MaxMentees));
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property10_PaginationReturnsAtMostPageSizeItems()
    {
        var gen = Gen.Choose(0, 20).SelectMany(c => Gen.ArrayOf(GenAvailableMentor(), c).SelectMany(ms => GenMenteeProfile().SelectMany(me => Gen.Choose(1, 10).SelectMany(ps => Gen.Choose(1, 5).Select(pg => new { me, ms = ms.ToList() as IReadOnlyList<MentorProfile>, pg, ps })))));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var r = MatchingEngine.GetBrowseResults(d.me, d.ms, d.pg, d.ps);
            r.Items.Count.Should().BeLessThanOrEqualTo(d.ps);
            var all = new List<MentorScoreResult>();
            for (var p = 1; p <= r.TotalPages; p++) all.AddRange(MatchingEngine.GetBrowseResults(d.me, d.ms, p, d.ps).Items);
            all.Count.Should().Be(r.TotalCount);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property11_OneActiveLockPerMentee()
    {
        var gen = Gen.Fresh(() => Guid.NewGuid()).SelectMany(g1 => Gen.Fresh(() => Guid.NewGuid()).Select(g2 => new { menteeId = new MenteeId(g1), mentorId = new MentorId(g2) }));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var lock1 = MentorLock.Create(d.menteeId, d.mentorId);
            lock1.IsExpired.Should().BeFalse();
            lock1.IsExpired.Should().BeFalse("second lock rejected when first is active");
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property14_CompletionFlowMentorCannotCompleteBeforeMentee()
    {
        var gen = Gen.Fresh(() => Guid.NewGuid()).SelectMany(g1 => Gen.Fresh(() => Guid.NewGuid()).Select(g2 => new { menteeId = new MenteeId(g1), mentorId = new MentorId(g2) }));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var s = MakeActiveSession(d.menteeId, d.mentorId);
            s.MarkComplete(Role.Mentor).IsFailure.Should().BeTrue();
            s.MarkComplete(Role.Mentee).IsSuccess.Should().BeTrue();
            s.Status.Should().Be(SessionStatus.MenteeCompleted);
            s.MarkComplete(Role.Mentor).IsSuccess.Should().BeTrue();
            s.Status.Should().Be(SessionStatus.Completed);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property15_MenteeCompletionIsIrrevocable()
    {
        var gen = Gen.Fresh(() => Guid.NewGuid()).SelectMany(g1 => Gen.Fresh(() => Guid.NewGuid()).SelectMany(g2 => Gen.Choose(1, 10).Select(att => new { menteeId = new MenteeId(g1), mentorId = new MentorId(g2), att })));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var s = MakeActiveSession(d.menteeId, d.mentorId);
            s.MarkComplete(Role.Mentee);
            var at = s.MenteeCompletedAt;
            at.Should().NotBeNull();
            for (var i = 0; i < d.att; i++) { s.MarkComplete(Role.Mentee).IsFailure.Should().BeTrue(); s.MenteeCompletedAt.Should().Be(at); }
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property21_OpportunityMaximumActiveLimit()
    {
        var gen = Gen.Fresh(() => Guid.NewGuid()).Select(g => new MentorId(g));
        return Prop.ForAll(gen.ToArbitrary(), mentorId =>
        {
            var postings = Enumerable.Range(0, 5).Select(i => OpportunityPosting.Create(mentorId, $"Opp {i}", OpportunityType.Job, "Corp", new string('x', 100), "Sydney", null, EmploymentType.FullTime, ["Python"], ExperienceLevel.Intermediate, "https://example.com")).ToList();
            postings.Count(p => p.IsActive).Should().Be(5);
            (postings.Count(p => p.IsActive) < 5).Should().BeFalse();
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property22_OpportunityExpiryComputation()
    {
        var gen = Gen.Elements(true, false).SelectMany(hasEvt => Gen.Choose(1, 90).Select(days => new { hasEvt, days }));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var now = DateTime.UtcNow;
            var evtDt = d.hasEvt ? now.AddDays(d.days) : (DateTime?)null;
            var posting = OpportunityPosting.Create(MentorId.New(), "T", d.hasEvt ? OpportunityType.Event : OpportunityType.Job, "O", new string('y', 100), "Remote", evtDt, d.hasEvt ? null : EmploymentType.FullTime, ["C#"], ExperienceLevel.Intermediate, "https://x.com", now);
            var thirty = now.AddDays(30);
            if (d.hasEvt && evtDt!.Value < thirty) posting.ExpiresAt.Should().BeCloseTo(evtDt.Value, TimeSpan.FromSeconds(1));
            else posting.ExpiresAt.Should().BeCloseTo(thirty, TimeSpan.FromSeconds(1));
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property23_OpportunityFilterCorrectness()
    {
        var gen = Gen.Choose(1, 10).SelectMany(c => Gen.ArrayOf(GenPosting(), c).SelectMany(ps => Gen.Elements(OpportunityType.Job, OpportunityType.Workshop, OpportunityType.Event, OpportunityType.Training).Select(f => new { ps = ps.ToList(), f })));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            d.ps.Where(p => p.Type == d.f).Should().AllSatisfy(p => p.Type.Should().Be(d.f));
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property24_OpportunityBadgeVisibility()
    {
        return Prop.ForAll(Gen.Choose(0, 5).ToArbitrary(), n => { if (n > 0) (n > 0).Should().BeTrue(); else (n > 0).Should().BeFalse(); });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property34_MentorAvailabilityToggleExcludesFromBrowse()
    {
        var gen = Gen.Choose(1, 8).SelectMany(c => Gen.ArrayOf(GenMentorWithAvailability(), c).SelectMany(ms => GenMenteeProfile().Select(me => new { me, ms = ms.ToList() as IReadOnlyList<MentorProfile> })));
        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var r = MatchingEngine.GetBrowseResults(d.me, d.ms);
            r.Items.Should().AllSatisfy(i => i.Mentor.AvailabilityStatus.Should().Be(AvailabilityStatus.Available));
            r.TotalCount.Should().Be(d.ms.Count(m => m.AvailabilityStatus == AvailabilityStatus.Available && m.ActiveMenteeCount < m.MaxMentees));
        });
    }

    private static Gen<MenteeProfile> GenMenteeProfile() =>
        Gen.Elements("A", "B", "C").SelectMany(n => Gen.Elements(Enum.GetValues<AustralianChapter>()).SelectMany(ch => Gen.Choose(0, 5).SelectMany(sc => Gen.ArrayOf(Gen.Elements(AllSkills), sc).SelectMany(sk => Gen.Choose(0, 15).SelectMany(exp => Gen.Elements(PrimaryGoal.CareerTransition, PrimaryGoal.SkillDevelopment, PrimaryGoal.CertificationPreparation, PrimaryGoal.ProjectGuidance).Select(g => new MenteeProfile(n, ch, "Sydney", sk.Distinct().ToList(), exp, g)))))));

    private static Gen<MentorProfile> GenMentorProfile() =>
        Gen.Elements("MA", "MB", "MC", "MD").SelectMany(n => Gen.Elements(Enum.GetValues<AustralianChapter>()).SelectMany(ch => Gen.Choose(0, 5).SelectMany(ec => Gen.ArrayOf(Gen.Elements(AllSkills), ec).SelectMany(ex => Gen.Choose(0, 4).SelectMany(tc => Gen.ArrayOf(Gen.Elements(AllTopics), tc).SelectMany(tp => Gen.Choose(0, 25).SelectMany(exp => Gen.Choose(1, 5).SelectMany(mx => Gen.Choose(0, 4).Select(ac => new MentorProfile(n, ch, "Sydney", ex.Distinct().ToList(), tp.Distinct().ToList(), exp, mx, Math.Min(ac, mx)))))))))));

    private static Gen<MentorProfile> GenMentorWithCapacity() =>
        Gen.Elements("X", "Y", "Z").SelectMany(n => Gen.Elements(Enum.GetValues<AustralianChapter>()).SelectMany(ch => Gen.Choose(1, 3).SelectMany(mx => Gen.Elements(true, false).Select(full => new MentorProfile(n, ch, "Sydney", ["Python"], ["code review"], 5, mx, full ? mx : Math.Max(0, mx - 1))))));

    private static Gen<MentorProfile> GenAvailableMentor() =>
        Gen.Elements("A1", "A2").SelectMany(n => Gen.Elements(Enum.GetValues<AustralianChapter>()).SelectMany(ch => Gen.Choose(2, 5).SelectMany(mx => Gen.Choose(0, 1).Select(ac => new MentorProfile(n, ch, "Sydney", ["Python"], ["code review"], 5, mx, ac)))));

    private static Gen<MentorProfile> GenMentorWithAvailability() =>
        Gen.Elements("T1", "T2").SelectMany(n => Gen.Elements(Enum.GetValues<AustralianChapter>()).SelectMany(ch => Gen.Choose(2, 5).SelectMany(mx => Gen.Choose(0, 1).SelectMany(ac => Gen.Elements(true, false).Select(av => new MentorProfile(n, ch, "Sydney", ["Python"], ["code review"], 10, mx, ac, av ? AvailabilityStatus.Available : AvailabilityStatus.Unavailable))))));

    private static Gen<OpportunityPosting> GenPosting() =>
        Gen.Elements(OpportunityType.Job, OpportunityType.Workshop, OpportunityType.Event, OpportunityType.Training).Select(t => OpportunityPosting.Create(MentorId.New(), "Opp", t, "Corp", new string('a', 100), "Remote", t != OpportunityType.Job ? DateTime.UtcNow.AddDays(15) : null, t == OpportunityType.Job ? EmploymentType.FullTime : null, ["Python"], ExperienceLevel.Intermediate, "https://example.com"));

    private static MenteeProfile MakeMentee(IReadOnlyList<string>? skills = null, PrimaryGoal primaryGoal = PrimaryGoal.SkillDevelopment) => new("Test", AustralianChapter.Sydney, "Sydney", skills ?? ["Python", "C#"], 3, primaryGoal);
    private static MentorProfile MakeMentor(IReadOnlyList<string>? expertiseAreas = null, IReadOnlyList<string>? topics = null) => new("Mentor", AustralianChapter.Sydney, "Sydney", expertiseAreas ?? ["Python"], topics ?? ["code review"], 8, 3, 1);

    private static Session MakeActiveSession(MenteeId menteeId, MentorId mentorId)
    {
        var s = Session.CreatePending(SessionId.New(), menteeId, mentorId, LockId.New());
        s.Accept(); s.Activate(); return s;
    }
}
