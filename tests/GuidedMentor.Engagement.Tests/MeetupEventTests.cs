using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Events;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Tests;

/// <summary>
/// Unit tests for the MeetupEvent domain entity.
/// Validates: Requirements 29.1, 29.7, 29.6
/// </summary>
public sealed class MeetupEventTests
{
    private static MeetupEvent CreateValidMeetupEvent(
        UserId? createdBy = null,
        AustralianChapter chapter = AustralianChapter.Sydney,
        DateTime? eventDate = null)
    {
        return MeetupEvent.Create(
            createdBy: createdBy ?? new UserId(Guid.NewGuid()),
            chapter: chapter,
            title: "AWS Sydney Meetup - Serverless Deep Dive",
            eventDate: eventDate ?? DateTime.UtcNow.AddDays(14),
            startTime: new TimeOnly(18, 0),
            endTime: new TimeOnly(20, 0),
            venueName: "AWS Office Sydney",
            venueAddress: "200 George St, Sydney NSW 2000",
            eventUrl: "https://meetup.com/aws-sydney/events/12345");
    }

    [Fact]
    public void Create_ValidInputs_ReturnsEventWithCorrectProperties()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var eventDate = DateTime.UtcNow.AddDays(7);

        var meetup = MeetupEvent.Create(
            createdBy: createdBy,
            chapter: AustralianChapter.Melbourne,
            title: "Melbourne AWS Meetup",
            eventDate: eventDate,
            startTime: new TimeOnly(18, 30),
            endTime: new TimeOnly(20, 30),
            venueName: "Microsoft Reactor",
            venueAddress: "11 Queens Rd, Melbourne VIC 3004",
            eventUrl: "https://meetup.com/aws-melbourne/events/99999");

        meetup.Id.Should().NotBe(default(MeetupEventId));
        meetup.CreatedBy.Should().Be(createdBy);
        meetup.Chapter.Should().Be(AustralianChapter.Melbourne);
        meetup.Title.Should().Be("Melbourne AWS Meetup");
        meetup.EventDate.Should().Be(eventDate);
        meetup.StartTime.Should().Be(new TimeOnly(18, 30));
        meetup.EndTime.Should().Be(new TimeOnly(20, 30));
        meetup.VenueName.Should().Be("Microsoft Reactor");
        meetup.VenueAddress.Should().Be("11 Queens Rd, Melbourne VIC 3004");
        meetup.EventUrl.Should().Be("https://meetup.com/aws-melbourne/events/99999");
        meetup.IsCancelled.Should().BeFalse();
        meetup.ConfirmedAttendees.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_ByCreator_Succeeds()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var meetup = CreateValidMeetupEvent(createdBy: createdBy);

        var result = meetup.Cancel(createdBy);

        result.IsSuccess.Should().BeTrue();
        meetup.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void Cancel_ByCreator_RaisesMeetupEventCancelledDomainEvent()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var meetup = CreateValidMeetupEvent(createdBy: createdBy);

        meetup.Cancel(createdBy);

        meetup.DomainEvents.Should().HaveCount(1);
        meetup.DomainEvents[0].Should().BeOfType<MeetupEventCancelledEvent>();
    }

    [Fact]
    public void Cancel_ByNonCreator_Fails()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var otherUser = new UserId(Guid.NewGuid());
        var meetup = CreateValidMeetupEvent(createdBy: createdBy);

        var result = meetup.Cancel(otherUser);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("chapter lead who created");
        meetup.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Fails()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var meetup = CreateValidMeetupEvent(createdBy: createdBy);
        meetup.Cancel(createdBy);

        var result = meetup.Cancel(createdBy);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already cancelled");
    }

    [Fact]
    public void ConfirmAttendance_ValidMentor_Succeeds()
    {
        var meetup = CreateValidMeetupEvent();
        var mentorId = new MentorId(Guid.NewGuid());

        var result = meetup.ConfirmAttendance(mentorId);

        result.IsSuccess.Should().BeTrue();
        meetup.ConfirmedAttendees.Should().Contain(mentorId);
        meetup.ConfirmedAttendees.Should().HaveCount(1);
    }

    [Fact]
    public void ConfirmAttendance_DuplicateMentor_Fails()
    {
        var meetup = CreateValidMeetupEvent();
        var mentorId = new MentorId(Guid.NewGuid());
        meetup.ConfirmAttendance(mentorId);

        var result = meetup.ConfirmAttendance(mentorId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already confirmed");
    }

    [Fact]
    public void ConfirmAttendance_CancelledEvent_Fails()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var meetup = CreateValidMeetupEvent(createdBy: createdBy);
        meetup.Cancel(createdBy);
        var mentorId = new MentorId(Guid.NewGuid());

        var result = meetup.ConfirmAttendance(mentorId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelled");
    }

    [Fact]
    public void ConfirmAttendance_PastEvent_Fails()
    {
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var meetup = MeetupEvent.Reconstitute(
            id: MeetupEventId.New(),
            createdBy: new UserId(Guid.NewGuid()),
            chapter: AustralianChapter.Sydney,
            title: "Past Event",
            eventDate: pastDate,
            startTime: new TimeOnly(18, 0),
            endTime: new TimeOnly(20, 0),
            venueName: "Venue",
            venueAddress: "123 Street",
            eventUrl: "https://meetup.com/event",
            isCancelled: false,
            confirmedAttendees: [],
            createdAt: pastDate.AddDays(-7));

        var result = meetup.ConfirmAttendance(new MentorId(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("past event");
    }

    [Fact]
    public void WithdrawAttendance_ConfirmedMentor_Succeeds()
    {
        var meetup = CreateValidMeetupEvent();
        var mentorId = new MentorId(Guid.NewGuid());
        meetup.ConfirmAttendance(mentorId);

        var result = meetup.WithdrawAttendance(mentorId);

        result.IsSuccess.Should().BeTrue();
        meetup.ConfirmedAttendees.Should().NotContain(mentorId);
    }

    [Fact]
    public void WithdrawAttendance_NotConfirmed_Fails()
    {
        var meetup = CreateValidMeetupEvent();
        var mentorId = new MentorId(Guid.NewGuid());

        var result = meetup.WithdrawAttendance(mentorId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not confirmed");
    }

    [Fact]
    public void WithdrawAttendance_CancelledEvent_Fails()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var meetup = CreateValidMeetupEvent(createdBy: createdBy);
        var mentorId = new MentorId(Guid.NewGuid());
        meetup.ConfirmAttendance(mentorId);
        meetup.Cancel(createdBy);

        var result = meetup.WithdrawAttendance(mentorId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelled");
    }

    [Fact]
    public void ConfirmAttendance_MultipleMentors_AllAdded()
    {
        var meetup = CreateValidMeetupEvent();
        var mentor1 = new MentorId(Guid.NewGuid());
        var mentor2 = new MentorId(Guid.NewGuid());
        var mentor3 = new MentorId(Guid.NewGuid());

        meetup.ConfirmAttendance(mentor1);
        meetup.ConfirmAttendance(mentor2);
        meetup.ConfirmAttendance(mentor3);

        meetup.ConfirmedAttendees.Should().HaveCount(3);
        meetup.ConfirmedAttendees.Should().Contain(mentor1);
        meetup.ConfirmedAttendees.Should().Contain(mentor2);
        meetup.ConfirmedAttendees.Should().Contain(mentor3);
    }
}
