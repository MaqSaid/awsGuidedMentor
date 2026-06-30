using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Tests;

/// <summary>
/// Unit tests for the Notification domain entity.
/// Validates: Requirements 12.2, 12.4
/// </summary>
public sealed class NotificationEntityTests
{
    // --- Notification.Create Tests ---

    [Fact]
    public void Create_ValidInput_ReturnsSuccessWithCorrectProperties()
    {
        var recipientId = new UserId(Guid.NewGuid());
        var result = Notification.Create(
            recipientId,
            NotificationType.SessionPlanReady,
            "Your session plan is ready!",
            "/sessions/plan/abc123");

        result.IsSuccess.Should().BeTrue();
        result.Value.RecipientUserId.Should().Be(recipientId);
        result.Value.Type.Should().Be(NotificationType.SessionPlanReady);
        result.Value.Message.Should().Be("Your session plan is ready!");
        result.Value.ActionUrl.Should().Be("/sessions/plan/abc123");
        result.Value.IsRead.Should().BeFalse();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_NullRecipient_ReturnsFailure()
    {
        var result = Notification.Create(
            null!,
            NotificationType.Reminder,
            "Test message",
            "/test");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Recipient");
    }

    [Fact]
    public void Create_EmptyMessage_ReturnsFailure()
    {
        var result = Notification.Create(
            new UserId(Guid.NewGuid()),
            NotificationType.RequestSent,
            "",
            "/test");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("message");
    }

    [Fact]
    public void Create_WhitespaceMessage_ReturnsFailure()
    {
        var result = Notification.Create(
            new UserId(Guid.NewGuid()),
            NotificationType.RequestSent,
            "   ",
            "/test");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("message");
    }

    [Fact]
    public void Create_MessageExactly500Chars_Succeeds()
    {
        var message = new string('A', 500);
        var result = Notification.Create(
            new UserId(Guid.NewGuid()),
            NotificationType.RequestAccepted,
            message,
            "/test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Message.Should().HaveLength(500);
    }

    [Fact]
    public void Create_MessageExceeds500Chars_ReturnsFailure()
    {
        var message = new string('A', 501);
        var result = Notification.Create(
            new UserId(Guid.NewGuid()),
            NotificationType.RequestDeclined,
            message,
            "/test");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("500");
    }

    [Fact]
    public void Create_EmptyActionUrl_ReturnsFailure()
    {
        var result = Notification.Create(
            new UserId(Guid.NewGuid()),
            NotificationType.CompletionMarked,
            "Valid message",
            "");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Action URL");
    }

    // --- MarkAsRead Tests ---

    [Fact]
    public void MarkAsRead_SetsIsReadToTrue()
    {
        var notification = Notification.Reconstitute(
            NotificationId.New(),
            new UserId(Guid.NewGuid()),
            NotificationType.RequestSent,
            "Test",
            "/test",
            isRead: false,
            DateTime.UtcNow);

        notification.IsRead.Should().BeFalse();
        notification.MarkAsRead();
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void MarkAsRead_AlreadyRead_RemainsTrue()
    {
        var notification = Notification.Reconstitute(
            NotificationId.New(),
            new UserId(Guid.NewGuid()),
            NotificationType.RequestSent,
            "Test",
            "/test",
            isRead: true,
            DateTime.UtcNow);

        notification.MarkAsRead();
        notification.IsRead.Should().BeTrue();
    }

    // --- Reconstitute Tests ---

    [Fact]
    public void Reconstitute_PreservesAllFields()
    {
        var id = NotificationId.New();
        var userId = new UserId(Guid.NewGuid());
        var createdAt = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        var notification = Notification.Reconstitute(
            id, userId,
            NotificationType.CompletionMarked,
            "Session completed",
            "/sessions/completed/xyz",
            isRead: true,
            createdAt);

        notification.Id.Should().Be(id);
        notification.RecipientUserId.Should().Be(userId);
        notification.Type.Should().Be(NotificationType.CompletionMarked);
        notification.Message.Should().Be("Session completed");
        notification.ActionUrl.Should().Be("/sessions/completed/xyz");
        notification.IsRead.Should().BeTrue();
        notification.CreatedAt.Should().Be(createdAt);
    }

    // --- NotificationType enum covers all required types ---

    [Fact]
    public void NotificationType_HasAllRequiredValues()
    {
        var values = Enum.GetValues<NotificationType>();
        values.Should().Contain(NotificationType.RequestSent);
        values.Should().Contain(NotificationType.RequestAccepted);
        values.Should().Contain(NotificationType.RequestDeclined);
        values.Should().Contain(NotificationType.SessionPlanReady);
        values.Should().Contain(NotificationType.CompletionMarked);
        values.Should().Contain(NotificationType.Reminder);
    }
}
