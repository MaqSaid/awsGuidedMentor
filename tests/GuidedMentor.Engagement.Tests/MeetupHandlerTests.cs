using GuidedMentor.Engagement.Application.Commands.Meetups;
using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Application.Queries.Meetups;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using NSubstitute;

namespace GuidedMentor.Engagement.Tests;

/// <summary>
/// Unit tests for Meetup Calendar command and query handlers.
/// Validates: Requirements 29.1, 29.7, 29.8, 29.3, 29.5, 29.9
/// </summary>
public sealed class MeetupHandlerTests
{
    private readonly IMeetupEventRepository _mockRepository;
    private readonly IChapterLeadValidator _mockChapterLeadValidator;
    private readonly IMeetupNotificationPublisher _mockNotificationPublisher;
    private readonly ISessionInfoProvider _mockSessionInfoProvider;

    public MeetupHandlerTests()
    {
        _mockRepository = Substitute.For<IMeetupEventRepository>();
        _mockChapterLeadValidator = Substitute.For<IChapterLeadValidator>();
        _mockNotificationPublisher = Substitute.For<IMeetupNotificationPublisher>();
        _mockSessionInfoProvider = Substitute.For<ISessionInfoProvider>();
    }

    private static MeetupEvent CreateTestMeetupEvent(
        UserId? createdBy = null,
        AustralianChapter chapter = AustralianChapter.Sydney,
        DateTime? eventDate = null,
        bool isCancelled = false)
    {
        var creator = createdBy ?? new UserId(Guid.NewGuid());
        return MeetupEvent.Reconstitute(
            id: MeetupEventId.New(),
            createdBy: creator,
            chapter: chapter,
            title: "Test Meetup Event",
            eventDate: eventDate ?? DateTime.UtcNow.AddDays(7),
            startTime: new TimeOnly(18, 0),
            endTime: new TimeOnly(20, 0),
            venueName: "Test Venue",
            venueAddress: "123 Test Street",
            eventUrl: "https://meetup.com/test",
            isCancelled: isCancelled,
            confirmedAttendees: [],
            createdAt: DateTime.UtcNow.AddDays(-1));
    }

    // --- CreateMeetupEventHandler Tests ---

    [Fact]
    public async Task CreateMeetupEvent_ChapterLead_Succeeds()
    {
        var chapterLeadId = Guid.NewGuid();
        _mockChapterLeadValidator
            .IsChapterLeadAsync(chapterLeadId, AustralianChapter.Sydney, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CreateMeetupEventHandler(_mockRepository, _mockChapterLeadValidator);
        var command = new CreateMeetupEventCommand(
            ChapterLeadId: chapterLeadId,
            Chapter: AustralianChapter.Sydney,
            Title: "Sydney AWS Meetup",
            EventDate: DateTime.UtcNow.AddDays(14),
            StartTime: new TimeOnly(18, 0),
            EndTime: new TimeOnly(20, 0),
            VenueName: "AWS Office",
            VenueAddress: "200 George St, Sydney NSW 2000",
            EventUrl: "https://meetup.com/aws-sydney/events/123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _mockRepository.Received(1).SaveAsync(Arg.Any<MeetupEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMeetupEvent_NotChapterLead_Fails()
    {
        var userId = Guid.NewGuid();
        _mockChapterLeadValidator
            .IsChapterLeadAsync(userId, AustralianChapter.Sydney, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CreateMeetupEventHandler(_mockRepository, _mockChapterLeadValidator);
        var command = new CreateMeetupEventCommand(
            ChapterLeadId: userId,
            Chapter: AustralianChapter.Sydney,
            Title: "Sydney AWS Meetup",
            EventDate: DateTime.UtcNow.AddDays(14),
            StartTime: new TimeOnly(18, 0),
            EndTime: new TimeOnly(20, 0),
            VenueName: "Venue",
            VenueAddress: "123 Street",
            EventUrl: "https://meetup.com/event");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("chapter leads");
        await _mockRepository.DidNotReceive().SaveAsync(Arg.Any<MeetupEvent>(), Arg.Any<CancellationToken>());
    }

    // --- CancelMeetupEventHandler Tests ---

    [Fact]
    public async Task CancelMeetupEvent_ChapterLead_Succeeds_NotifiesAffected()
    {
        var chapterLeadId = Guid.NewGuid();
        var meetup = CreateTestMeetupEvent(createdBy: new UserId(chapterLeadId));

        _mockRepository
            .GetByIdAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(meetup);
        _mockChapterLeadValidator
            .IsChapterLeadAsync(chapterLeadId, AustralianChapter.Sydney, Arg.Any<CancellationToken>())
            .Returns(true);
        _mockRepository
            .GetAlignedSessionIdsAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        var handler = new CancelMeetupEventHandler(
            _mockRepository, _mockChapterLeadValidator, _mockNotificationPublisher);
        var command = new CancelMeetupEventCommand(meetup.Id.Value, chapterLeadId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mockRepository.Received(1).SaveAsync(meetup, Arg.Any<CancellationToken>());
        await _mockNotificationPublisher.Received(1).NotifyMeetupCancelledAsync(
            meetup.Id.Value,
            meetup.Title,
            Arg.Any<IReadOnlyList<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelMeetupEvent_NotChapterLead_Fails()
    {
        var chapterLeadId = Guid.NewGuid();
        var nonLeadId = Guid.NewGuid();
        var meetup = CreateTestMeetupEvent(createdBy: new UserId(chapterLeadId));

        _mockRepository
            .GetByIdAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(meetup);
        _mockChapterLeadValidator
            .IsChapterLeadAsync(nonLeadId, AustralianChapter.Sydney, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CancelMeetupEventHandler(
            _mockRepository, _mockChapterLeadValidator, _mockNotificationPublisher);
        var command = new CancelMeetupEventCommand(meetup.Id.Value, nonLeadId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("chapter leads");
    }

    [Fact]
    public async Task CancelMeetupEvent_EventNotFound_Fails()
    {
        _mockRepository
            .GetByIdAsync(Arg.Any<MeetupEventId>(), Arg.Any<CancellationToken>())
            .Returns((MeetupEvent?)null);

        var handler = new CancelMeetupEventHandler(
            _mockRepository, _mockChapterLeadValidator, _mockNotificationPublisher);
        var command = new CancelMeetupEventCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CancelMeetupEvent_NoAlignedSessions_DoesNotNotify()
    {
        var chapterLeadId = Guid.NewGuid();
        var meetup = CreateTestMeetupEvent(createdBy: new UserId(chapterLeadId));

        _mockRepository
            .GetByIdAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(meetup);
        _mockChapterLeadValidator
            .IsChapterLeadAsync(chapterLeadId, AustralianChapter.Sydney, Arg.Any<CancellationToken>())
            .Returns(true);
        _mockRepository
            .GetAlignedSessionIdsAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var handler = new CancelMeetupEventHandler(
            _mockRepository, _mockChapterLeadValidator, _mockNotificationPublisher);
        var command = new CancelMeetupEventCommand(meetup.Id.Value, chapterLeadId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mockNotificationPublisher.DidNotReceive().NotifyMeetupCancelledAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
    }

    // --- ConfirmMeetupAttendanceHandler Tests ---

    [Fact]
    public async Task ConfirmAttendance_ValidMentor_Succeeds()
    {
        var meetup = CreateTestMeetupEvent();
        _mockRepository
            .GetByIdAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(meetup);

        var handler = new ConfirmMeetupAttendanceHandler(_mockRepository);
        var command = new ConfirmMeetupAttendanceCommand(meetup.Id.Value, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mockRepository.Received(1).SaveAsync(meetup, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmAttendance_EventNotFound_Fails()
    {
        _mockRepository
            .GetByIdAsync(Arg.Any<MeetupEventId>(), Arg.Any<CancellationToken>())
            .Returns((MeetupEvent?)null);

        var handler = new ConfirmMeetupAttendanceHandler(_mockRepository);
        var command = new ConfirmMeetupAttendanceCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // --- WithdrawMeetupAttendanceHandler Tests ---

    [Fact]
    public async Task WithdrawAttendance_ConfirmedMentor_Succeeds()
    {
        var mentorId = Guid.NewGuid();
        var meetup = CreateTestMeetupEvent();
        meetup.ConfirmAttendance(new MentorId(mentorId));

        _mockRepository
            .GetByIdAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(meetup);

        var handler = new WithdrawMeetupAttendanceHandler(_mockRepository);
        var command = new WithdrawMeetupAttendanceCommand(meetup.Id.Value, mentorId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mockRepository.Received(1).SaveAsync(meetup, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawAttendance_NotConfirmed_Fails()
    {
        var meetup = CreateTestMeetupEvent();
        _mockRepository
            .GetByIdAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(meetup);

        var handler = new WithdrawMeetupAttendanceHandler(_mockRepository);
        var command = new WithdrawMeetupAttendanceCommand(meetup.Id.Value, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not confirmed");
    }

    // --- AlignSessionToMeetupHandler Tests ---

    [Fact]
    public async Task AlignSession_ValidMeetup_Succeeds()
    {
        var meetup = CreateTestMeetupEvent();
        _mockRepository
            .GetByIdAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(meetup);

        var handler = new AlignSessionToMeetupHandler(_mockRepository);
        var sessionId = Guid.NewGuid();
        var command = new AlignSessionToMeetupCommand(sessionId, meetup.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mockRepository.Received(1).AlignSessionAsync(
            sessionId, meetup.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AlignSession_CancelledMeetup_Fails()
    {
        var createdBy = new UserId(Guid.NewGuid());
        var meetup = CreateTestMeetupEvent(createdBy: createdBy, isCancelled: true);
        _mockRepository
            .GetByIdAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(meetup);

        var handler = new AlignSessionToMeetupHandler(_mockRepository);
        var command = new AlignSessionToMeetupCommand(Guid.NewGuid(), meetup.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelled");
    }

    [Fact]
    public async Task AlignSession_PastMeetup_Fails()
    {
        var meetup = MeetupEvent.Reconstitute(
            id: MeetupEventId.New(),
            createdBy: new UserId(Guid.NewGuid()),
            chapter: AustralianChapter.Sydney,
            title: "Past Meetup",
            eventDate: DateTime.UtcNow.AddDays(-2),
            startTime: new TimeOnly(18, 0),
            endTime: new TimeOnly(20, 0),
            venueName: "Venue",
            venueAddress: "123 Street",
            eventUrl: "https://meetup.com/event",
            isCancelled: false,
            confirmedAttendees: [],
            createdAt: DateTime.UtcNow.AddDays(-10));

        _mockRepository
            .GetByIdAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(meetup);

        var handler = new AlignSessionToMeetupHandler(_mockRepository);
        var command = new AlignSessionToMeetupCommand(Guid.NewGuid(), meetup.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("past");
    }

    [Fact]
    public async Task AlignSession_MeetupNotFound_Fails()
    {
        _mockRepository
            .GetByIdAsync(Arg.Any<MeetupEventId>(), Arg.Any<CancellationToken>())
            .Returns((MeetupEvent?)null);

        var handler = new AlignSessionToMeetupHandler(_mockRepository);
        var command = new AlignSessionToMeetupCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // --- GetUpcomingMeetupsHandler Tests ---

    [Fact]
    public async Task GetUpcomingMeetups_ReturnsCorrectDtos()
    {
        var meetups = new List<MeetupEvent>
        {
            CreateTestMeetupEvent(chapter: AustralianChapter.Brisbane, eventDate: DateTime.UtcNow.AddDays(3)),
            CreateTestMeetupEvent(chapter: AustralianChapter.Brisbane, eventDate: DateTime.UtcNow.AddDays(10))
        };

        _mockRepository
            .GetUpcomingByChapterAsync(AustralianChapter.Brisbane, 3, Arg.Any<CancellationToken>())
            .Returns(meetups);

        var handler = new GetUpcomingMeetupsHandler(_mockRepository);
        var query = new GetUpcomingMeetupsQuery(AustralianChapter.Brisbane);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Chapter.Should().Be(AustralianChapter.Brisbane);
        result.Value[0].IsCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task GetUpcomingMeetups_EmptyChapter_ReturnsEmptyList()
    {
        _mockRepository
            .GetUpcomingByChapterAsync(AustralianChapter.Darwin, 3, Arg.Any<CancellationToken>())
            .Returns(new List<MeetupEvent>());

        var handler = new GetUpcomingMeetupsHandler(_mockRepository);
        var query = new GetUpcomingMeetupsQuery(AustralianChapter.Darwin);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // --- SendMeetupSessionRemindersHandler Tests ---

    [Fact]
    public async Task SendReminders_MeetupsTomorrow_SendsNotifications()
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var meetup = MeetupEvent.Reconstitute(
            id: MeetupEventId.New(),
            createdBy: new UserId(Guid.NewGuid()),
            chapter: AustralianChapter.Sydney,
            title: "Tomorrow's Meetup",
            eventDate: tomorrow,
            startTime: new TimeOnly(18, 0),
            endTime: new TimeOnly(20, 0),
            venueName: "AWS Office",
            venueAddress: "200 George St",
            eventUrl: "https://meetup.com/event",
            isCancelled: false,
            confirmedAttendees: [],
            createdAt: DateTime.UtcNow.AddDays(-5));

        var sessionId = Guid.NewGuid();
        var mentorId = Guid.NewGuid();
        var menteeId = Guid.NewGuid();

        _mockRepository
            .GetUpcomingByChapterAsync(AustralianChapter.Sydney, 10, Arg.Any<CancellationToken>())
            .Returns(new List<MeetupEvent> { meetup });
        _mockRepository
            .GetAlignedSessionIdsAsync(meetup.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { sessionId });
        _mockSessionInfoProvider
            .GetSessionInfoAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new SessionInfo(sessionId, mentorId, menteeId, "Test Session"));

        var handler = new SendMeetupSessionRemindersHandler(
            _mockRepository, _mockNotificationPublisher, _mockSessionInfoProvider);
        var command = new SendMeetupSessionRemindersCommand(AustralianChapter.Sydney);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        await _mockNotificationPublisher.Received(1).SendMeetupSessionReminderAsync(
            sessionId, mentorId, menteeId, meetup.Id.Value,
            "AWS Office", "200 George St", "Test Session",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendReminders_NoMeetupsTomorrow_SendsZero()
    {
        var nextWeek = DateTime.UtcNow.Date.AddDays(7);
        var meetup = MeetupEvent.Reconstitute(
            id: MeetupEventId.New(),
            createdBy: new UserId(Guid.NewGuid()),
            chapter: AustralianChapter.Sydney,
            title: "Next Week Meetup",
            eventDate: nextWeek,
            startTime: new TimeOnly(18, 0),
            endTime: new TimeOnly(20, 0),
            venueName: "Venue",
            venueAddress: "Address",
            eventUrl: "https://meetup.com/event",
            isCancelled: false,
            confirmedAttendees: [],
            createdAt: DateTime.UtcNow.AddDays(-5));

        _mockRepository
            .GetUpcomingByChapterAsync(AustralianChapter.Sydney, 10, Arg.Any<CancellationToken>())
            .Returns(new List<MeetupEvent> { meetup });

        var handler = new SendMeetupSessionRemindersHandler(
            _mockRepository, _mockNotificationPublisher, _mockSessionInfoProvider);
        var command = new SendMeetupSessionRemindersCommand(AustralianChapter.Sydney);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        await _mockNotificationPublisher.DidNotReceive().SendMeetupSessionReminderAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
