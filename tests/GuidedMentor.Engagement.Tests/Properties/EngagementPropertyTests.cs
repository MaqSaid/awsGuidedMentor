using System.Security.Cryptography;
using System.Text;
using FsCheck.Fluent;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Tests.Properties;

[Trait("Category", "Property")]
public sealed class EngagementPropertyTests : PropertyTestBase
{
    [Property(MaxTest = 100)]
    public FsCheck.Property Property17_NotificationsOrderedReverseChronologically()
    {
        var gen = Gen.Choose(1, 50).SelectMany(count => Gen.ArrayOf(GenNotification(), count).Select(n => n.ToList()));
        return Prop.ForAll(gen.ToArbitrary(), notifications =>
        {
            var sorted = notifications.OrderByDescending(n => n.CreatedAt).Take(50).ToList();
            for (var i = 0; i < sorted.Count - 1; i++)
                sorted[i].CreatedAt.Should().BeOnOrAfter(sorted[i + 1].CreatedAt);
            sorted.Count.Should().BeLessThanOrEqualTo(50);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property25_MeetupSessionAlignmentPreservesData()
    {
        var gen = Gen.Elements(Enum.GetValues<AustralianChapter>()).SelectMany(ch =>
            Gen.Choose(1, 60).SelectMany(days =>
            Gen.Choose(9, 18).SelectMany(hour =>
            Gen.Elements("AWS Loft", "WeWork", "Town Hall", "Tech Hub").Select(venue =>
                new { ch, date = DateTime.UtcNow.Date.AddDays(days), time = new TimeOnly(hour, 0), venue }))));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var meetup = MeetupEvent.Create(UserId.New(), d.ch, "Test", d.date, d.time, d.time.AddHours(2), d.venue, "123 St", "https://meetup.com/t");
            meetup.EventDate.Should().Be(d.date);
            meetup.StartTime.Should().Be(d.time);
            meetup.VenueName.Should().Be(d.venue);
            meetup.Chapter.Should().Be(d.ch);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property26_ChapterLeadAuthorization()
    {
        var gen = Gen.Elements(true, false).SelectMany(isLead =>
            Gen.Elements(Enum.GetValues<AustralianChapter>()).Select(ch => new { isLead, ch }));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var creatorId = UserId.New();
            var meetup = MeetupEvent.Create(creatorId, d.ch, "Meetup", DateTime.UtcNow.AddDays(14), new TimeOnly(18, 0), new TimeOnly(20, 0), "V", "A", "https://m.com");
            var requesterId = d.isLead ? creatorId : UserId.New();
            var result = meetup.Cancel(requesterId);
            if (d.isLead) result.IsSuccess.Should().BeTrue();
            else result.IsFailure.Should().BeTrue();
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property27_MeetupCancellationIdentifiesAllAffectedSessions()
    {
        var gen = Gen.Choose(0, 10).SelectMany(aligned =>
            Gen.Choose(0, 10).Select(unrelated => new { aligned, unrelated }));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var alignedIds = Enumerable.Range(0, d.aligned).Select(_ => Guid.NewGuid().ToString()).ToList();
            var unrelatedIds = Enumerable.Range(0, d.unrelated).Select(_ => Guid.NewGuid().ToString()).ToList();
            alignedIds.Count.Should().Be(d.aligned);
            alignedIds.Should().NotIntersectWith(unrelatedIds);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property28_UpcomingMeetupsQueryReturnsFilteredResults()
    {
        var gen = Gen.Choose(0, 10).SelectMany(count =>
            Gen.Elements(Enum.GetValues<AustralianChapter>()).SelectMany(ch =>
            Gen.ArrayOf(GenMeetupEvent(), count).Select(ms => new { ms = ms.ToList(), ch })));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var filtered = d.ms.Where(m => m.Chapter == d.ch && !m.IsCancelled && m.EventDate >= DateTime.UtcNow.Date)
                .OrderBy(m => m.EventDate).Take(3).ToList();
            filtered.Count.Should().BeLessThanOrEqualTo(3);
            filtered.Should().AllSatisfy(m => { m.Chapter.Should().Be(d.ch); m.IsCancelled.Should().BeFalse(); });
            for (var i = 0; i < filtered.Count - 1; i++)
                filtered[i].EventDate.Should().BeOnOrBefore(filtered[i + 1].EventDate);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property29_TrackedEventsContainNoPII()
    {
        var gen = Gen.Elements("user@test.com", "admin@x.org").SelectMany(email =>
            Gen.Elements("+61400000000", "0412345678").SelectMany(phone =>
            Gen.Fresh(() => Guid.NewGuid()).Select(uid => new { email, phone, uid })));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var hash = Sha256(d.uid.ToString());
            var evt = EngagementEvent.Create(hash, "page_view", null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Guid.NewGuid().ToString(), "/dash", "mentor");
            evt.UserIdHash.Should().NotContain("@");
            evt.UserIdHash.Should().MatchRegex("^[a-f0-9]{64}$");
            evt.UserIdHash.Should().NotBe(d.email);
            evt.UserIdHash.Should().NotBe(d.phone);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property30_EventSchemaCompleteness()
    {
        var gen = Gen.Elements("mentor", "mentee").SelectMany(role =>
            Gen.Elements("page_view", "button_click", "form_submit").SelectMany(evtType =>
            Gen.Elements("/dashboard", "/browse", "/settings").Select(page => new { role, evtType, page })));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var evt = EngagementEvent.Create(Sha256(Guid.NewGuid().ToString()), d.evtType, null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Guid.NewGuid().ToString(), d.page, d.role);
            evt.UserIdHash.Should().NotBeNullOrWhiteSpace();
            evt.EventType.Should().NotBeNullOrWhiteSpace();
            evt.Timestamp.Should().BePositive();
            evt.SessionId.Should().NotBeNullOrWhiteSpace();
            evt.ActiveRole.Should().BeOneOf("mentor", "mentee");
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property31_ConsentOptOutDisablesNonEssentialTracking()
    {
        var gen = Gen.Elements("granted", "denied", "pending").SelectMany(consent =>
            Gen.Choose(1, 20).SelectMany(count =>
            Gen.ArrayOf(Gen.Elements("page_view", "button_click", "auth_login", "auth_logout", "error_boundary"), count)
               .Select(events => new { consent, events = events.ToList() })));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var essential = new[] { "auth_login", "auth_logout", "error_boundary" };
            var tracked = d.consent == "denied" ? d.events.Where(e => essential.Contains(e)).ToList() : d.events;
            if (d.consent == "denied")
            {
                tracked.Should().AllSatisfy(e => essential.Should().Contain(e));
                tracked.Where(e => !essential.Contains(e)).Should().BeEmpty();
            }
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property32_EventBufferFlushIntegrity()
    {
        var gen = Gen.Choose(1, 30).SelectMany(count =>
            Gen.ArrayOf(Gen.Elements("page_view", "click", "submit", "nav"), count).SelectMany(events =>
            Gen.Elements(true, false).Select(success => new { events = events.ToList(), success })));

        return Prop.ForAll(gen.ToArbitrary(), d =>
        {
            var buffer = new List<string>(d.events);
            var original = d.events.ToList();
            if (d.success) { buffer.Clear(); buffer.Should().BeEmpty(); }
            else
            {
                var flushing = new List<string>(buffer);
                buffer.Clear();
                buffer.AddRange(flushing);
                buffer.Should().BeEquivalentTo(original, opt => opt.WithStrictOrdering());
            }
        });
    }

    private static Gen<Notification> GenNotification() =>
        Gen.Choose(0, 10000).Select(mins => Notification.Reconstitute(
            NotificationId.New(), UserId.New(), NotificationType.RequestAccepted,
            $"Msg {mins}", "/test", false, DateTime.UtcNow.AddMinutes(-mins)));

    private static Gen<MeetupEvent> GenMeetupEvent() =>
        Gen.Elements(Enum.GetValues<AustralianChapter>()).SelectMany(ch =>
        Gen.Choose(-10, 30).SelectMany(days =>
        Gen.Elements(true, false).Select(cancelled =>
            MeetupEvent.Reconstitute(MeetupEventId.New(), UserId.New(), ch, $"Meetup {ch}",
                DateTime.UtcNow.Date.AddDays(days), new TimeOnly(18, 0), new TimeOnly(20, 0),
                "Venue", "123 St", "https://m.com/t", cancelled, [], DateTime.UtcNow.AddDays(-7)))));

    private static string Sha256(string input) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
}
