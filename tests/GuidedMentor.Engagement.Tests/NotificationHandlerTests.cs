using GuidedMentor.Engagement.Application.Commands.Notifications;
using GuidedMentor.Engagement.Application.DTOs;
using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Application.Queries.Notifications;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using NSubstitute;

namespace GuidedMentor.Engagement.Tests;

/// <summary>
/// Unit tests for Notification command and query handlers.
/// Validates: Requirements 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7
/// </summary>
public sealed class NotificationHandlerTests
{
    private readonly INotificationRepository _mockRepository;
    private readonly IAppSyncNotificationPublisher _mockPublisher;

    public NotificationHandlerTests()
    {
        _mockRepository = Substitute.For<INotificationRepository>();
        _mockPublisher = Substitute.For<IAppSyncNotificationPublisher>();
    }

    // --- CreateNotificationHandler Tests ---

    [Fact]
    public async Task CreateNotification_ValidInput_PersistsAndPublishes()
    {
        var handler = new CreateNotificationHandler(_mockRepository, _mockPublisher);
        var command = new CreateNotificationCommand(
            RecipientUserId: Guid.NewGuid(),
            Type: NotificationType.RequestSent,
            Message: "You received a mentorship request.",
            ActionUrl: "/sessions/pending");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mockRepository.Received(1).SaveAsync(
            Arg.Is<Notification>(n =>
                n.RecipientUserId.Value == command.RecipientUserId &&
                n.Type == NotificationType.RequestSent &&
                n.Message == command.Message &&
                n.ActionUrl == command.ActionUrl &&
                n.IsRead == false),
            Arg.Any<CancellationToken>());
        await _mockPublisher.Received(1).PublishAsync(
            Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotification_EmptyMessage_ReturnsFailure()
    {
        var handler = new CreateNotificationHandler(_mockRepository, _mockPublisher);
        var command = new CreateNotificationCommand(
            RecipientUserId: Guid.NewGuid(),
            Type: NotificationType.Reminder,
            Message: "",
            ActionUrl: "/dashboard");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("message");
        await _mockRepository.DidNotReceive().SaveAsync(
            Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _mockPublisher.DidNotReceive().PublishAsync(
            Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotification_MessageExceeds500Chars_ReturnsFailure()
    {
        var handler = new CreateNotificationHandler(_mockRepository, _mockPublisher);
        var command = new CreateNotificationCommand(
            RecipientUserId: Guid.NewGuid(),
            Type: NotificationType.SessionPlanReady,
            Message: new string('A', 501),
            ActionUrl: "/sessions/plan/123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("500");
        await _mockRepository.DidNotReceive().SaveAsync(
            Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotification_EmptyActionUrl_ReturnsFailure()
    {
        var handler = new CreateNotificationHandler(_mockRepository, _mockPublisher);
        var command = new CreateNotificationCommand(
            RecipientUserId: Guid.NewGuid(),
            Type: NotificationType.CompletionMarked,
            Message: "Your session was completed.",
            ActionUrl: "");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Action URL");
        await _mockRepository.DidNotReceive().SaveAsync(
            Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(NotificationType.RequestSent)]
    [InlineData(NotificationType.RequestAccepted)]
    [InlineData(NotificationType.RequestDeclined)]
    [InlineData(NotificationType.SessionPlanReady)]
    [InlineData(NotificationType.CompletionMarked)]
    [InlineData(NotificationType.Reminder)]
    public async Task CreateNotification_AllTypes_Succeed(NotificationType type)
    {
        var handler = new CreateNotificationHandler(_mockRepository, _mockPublisher);
        var command = new CreateNotificationCommand(
            RecipientUserId: Guid.NewGuid(),
            Type: type,
            Message: $"Test notification of type {type}",
            ActionUrl: "/test");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mockRepository.Received(1).SaveAsync(
            Arg.Is<Notification>(n => n.Type == type),
            Arg.Any<CancellationToken>());
    }

    // --- MarkNotificationReadHandler Tests ---

    [Fact]
    public async Task MarkNotificationRead_ExistingUnread_Succeeds()
    {
        var notificationId = Guid.NewGuid();
        var notification = Notification.Reconstitute(
            id: new NotificationId(notificationId),
            recipientUserId: new UserId(Guid.NewGuid()),
            type: NotificationType.RequestSent,
            message: "Test message",
            actionUrl: "/test",
            isRead: false,
            createdAt: DateTime.UtcNow);

        _mockRepository
            .GetByIdAsync(Arg.Is<NotificationId>(id => id.Value == notificationId), Arg.Any<CancellationToken>())
            .Returns(notification);

        var handler = new MarkNotificationReadHandler(_mockRepository);
        var command = new MarkNotificationReadCommand(notificationId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mockRepository.Received(1).MarkAsReadAsync(
            Arg.Is<NotificationId>(id => id.Value == notificationId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkNotificationRead_AlreadyRead_ReturnsSuccess()
    {
        var notificationId = Guid.NewGuid();
        var notification = Notification.Reconstitute(
            id: new NotificationId(notificationId),
            recipientUserId: new UserId(Guid.NewGuid()),
            type: NotificationType.RequestAccepted,
            message: "Already read",
            actionUrl: "/test",
            isRead: true,
            createdAt: DateTime.UtcNow);

        _mockRepository
            .GetByIdAsync(Arg.Is<NotificationId>(id => id.Value == notificationId), Arg.Any<CancellationToken>())
            .Returns(notification);

        var handler = new MarkNotificationReadHandler(_mockRepository);
        var command = new MarkNotificationReadCommand(notificationId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Should not call MarkAsReadAsync again if already read
        await _mockRepository.DidNotReceive().MarkAsReadAsync(
            Arg.Any<NotificationId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkNotificationRead_NotFound_ReturnsFailure()
    {
        _mockRepository
            .GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var handler = new MarkNotificationReadHandler(_mockRepository);
        var command = new MarkNotificationReadCommand(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // --- BatchMarkReadHandler Tests ---

    [Fact]
    public async Task BatchMarkRead_ValidUser_CallsRepository()
    {
        var userId = Guid.NewGuid();
        var handler = new BatchMarkReadHandler(_mockRepository);
        var command = new BatchMarkReadCommand(userId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _mockRepository.Received(1).BatchMarkAsReadAsync(
            Arg.Is<UserId>(id => id.Value == userId),
            Arg.Any<CancellationToken>());
    }

    // --- GetNotificationsHandler Tests ---

    [Fact]
    public async Task GetNotifications_ReturnsLast50_ReverseChronological()
    {
        var userId = Guid.NewGuid();
        var notifications = Enumerable.Range(0, 50)
            .Select(i => Notification.Reconstitute(
                id: NotificationId.New(),
                recipientUserId: new UserId(userId),
                type: NotificationType.RequestSent,
                message: $"Notification {i}",
                actionUrl: $"/test/{i}",
                isRead: i % 2 == 0,
                createdAt: DateTime.UtcNow.AddMinutes(-i)))
            .ToList();

        _mockRepository
            .GetByRecipientAsync(
                Arg.Is<UserId>(id => id.Value == userId),
                50,
                Arg.Any<CancellationToken>())
            .Returns(notifications);

        var handler = new GetNotificationsHandler(_mockRepository);
        var query = new GetNotificationsQuery(userId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(50);
        result[0].CreatedAt.Should().BeOnOrAfter(result[1].CreatedAt);
    }

    [Fact]
    public async Task GetNotifications_CapsLimitAt50()
    {
        var userId = Guid.NewGuid();
        _mockRepository
            .GetByRecipientAsync(
                Arg.Is<UserId>(id => id.Value == userId),
                50,
                Arg.Any<CancellationToken>())
            .Returns(new List<Notification>());

        var handler = new GetNotificationsHandler(_mockRepository);
        // Request 100 but should be capped at 50
        var query = new GetNotificationsQuery(userId, Limit: 100);

        await handler.Handle(query, CancellationToken.None);

        await _mockRepository.Received(1).GetByRecipientAsync(
            Arg.Any<UserId>(),
            50,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetNotifications_DistinguishesUnread()
    {
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            Notification.Reconstitute(
                NotificationId.New(), new UserId(userId),
                NotificationType.RequestSent, "Unread", "/test", false, DateTime.UtcNow),
            Notification.Reconstitute(
                NotificationId.New(), new UserId(userId),
                NotificationType.RequestAccepted, "Read", "/test", true, DateTime.UtcNow.AddMinutes(-5))
        };

        _mockRepository
            .GetByRecipientAsync(Arg.Any<UserId>(), 50, Arg.Any<CancellationToken>())
            .Returns(notifications);

        var handler = new GetNotificationsHandler(_mockRepository);
        var query = new GetNotificationsQuery(userId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].IsRead.Should().BeFalse();
        result[1].IsRead.Should().BeTrue();
    }

    // --- GetUnreadCountHandler Tests ---

    [Fact]
    public async Task GetUnreadCount_ReturnsRepositoryCount()
    {
        var userId = Guid.NewGuid();
        _mockRepository
            .GetUnreadCountAsync(Arg.Is<UserId>(id => id.Value == userId), Arg.Any<CancellationToken>())
            .Returns(7);

        var handler = new GetUnreadCountHandler(_mockRepository);
        var query = new GetUnreadCountQuery(userId);

        var count = await handler.Handle(query, CancellationToken.None);

        count.Should().Be(7);
    }

    [Fact]
    public async Task GetUnreadCount_ZeroUnread_ReturnsZero()
    {
        var userId = Guid.NewGuid();
        _mockRepository
            .GetUnreadCountAsync(Arg.Is<UserId>(id => id.Value == userId), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = new GetUnreadCountHandler(_mockRepository);
        var query = new GetUnreadCountQuery(userId);

        var count = await handler.Handle(query, CancellationToken.None);

        count.Should().Be(0);
    }
}
