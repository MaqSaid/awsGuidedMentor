using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Tests;

/// <summary>
/// Unit tests for the Session aggregate root's completion flow state machine.
/// Validates Requirements 9.1-9.7 (Two-Party Completion Flow).
/// </summary>
public sealed class SessionCompletionFlowTests
{
    private static Session CreateActiveSession()
    {
        var session = Session.CreatePending(
            SessionId.New(),
            MenteeId.New(),
            MentorId.New(),
            LockId.New());

        // Transition through Accept to PendingPlan
        session.Accept();
        // Transition to Active
        session.Activate();
        return session;
    }

    private static Session CreatePendingAcceptanceSession()
    {
        return Session.CreatePending(
            SessionId.New(),
            MenteeId.New(),
            MentorId.New(),
            LockId.New());
    }

    [Fact]
    public void CreatePending_ShouldInitializeWithPendingAcceptanceStatus()
    {
        var session = CreatePendingAcceptanceSession();

        session.Status.Should().Be(SessionStatus.PendingAcceptance);
        session.MenteeCompletedAt.Should().BeNull();
        session.MentorCompletedAt.Should().BeNull();
    }

    [Fact]
    public void Accept_WhenPendingAcceptance_ShouldTransitionToPendingPlan()
    {
        var session = CreatePendingAcceptanceSession();

        var result = session.Accept();

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.PendingPlan);
    }

    [Fact]
    public void Accept_WhenNotPendingAcceptance_ShouldFail()
    {
        var session = CreateActiveSession();

        var result = session.Accept();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("PendingAcceptance");
    }

    [Fact]
    public void Decline_WhenPendingAcceptance_ShouldTransitionToUnresolved()
    {
        var session = CreatePendingAcceptanceSession();

        var result = session.Decline();

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Unresolved);
    }

    [Fact]
    public void Decline_WhenNotPendingAcceptance_ShouldFail()
    {
        var session = CreateActiveSession();

        var result = session.Decline();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenPendingPlan_ShouldTransitionToActive()
    {
        var session = CreatePendingAcceptanceSession();
        session.Accept();

        var result = session.Activate();

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public void Activate_WhenNotPendingPlan_ShouldFail()
    {
        var session = CreateActiveSession();

        var result = session.Activate();

        result.IsFailure.Should().BeTrue();
    }

    // ── Completion State Machine Tests ──

    [Fact]
    public void MarkComplete_MenteeFirst_ShouldTransitionToMenteeCompleted()
    {
        var session = CreateActiveSession();

        var result = session.MarkComplete(Role.Mentee);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.MenteeCompleted);
        session.MenteeCompletedAt.Should().NotBeNull();
        session.MentorCompletedAt.Should().BeNull();
    }

    [Fact]
    public void MarkComplete_MentorAfterMentee_ShouldTransitionToCompleted()
    {
        var session = CreateActiveSession();
        session.MarkComplete(Role.Mentee);

        var result = session.MarkComplete(Role.Mentor);

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Completed);
        session.MenteeCompletedAt.Should().NotBeNull();
        session.MentorCompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkComplete_MentorFirst_ShouldBeRejected()
    {
        var session = CreateActiveSession();

        var result = session.MarkComplete(Role.Mentor);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("mentee must mark");
        session.Status.Should().Be(SessionStatus.Active);
        session.MentorCompletedAt.Should().BeNull();
    }

    [Fact]
    public void MarkComplete_MenteeRetraction_ShouldBeRejected()
    {
        var session = CreateActiveSession();
        session.MarkComplete(Role.Mentee);

        // Mentee tries to mark again (effectively a retraction attempt)
        var result = session.MarkComplete(Role.Mentee);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already marked");
        session.Status.Should().Be(SessionStatus.MenteeCompleted);
    }

    [Fact]
    public void MarkComplete_MenteeOnNonActiveSession_ShouldBeRejected()
    {
        var session = CreatePendingAcceptanceSession();

        var result = session.MarkComplete(Role.Mentee);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Active status");
    }

    [Fact]
    public void MarkComplete_MentorRetraction_ShouldBeRejected()
    {
        var session = CreateActiveSession();
        session.MarkComplete(Role.Mentee);
        session.MarkComplete(Role.Mentor);

        var result = session.MarkComplete(Role.Mentor);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already confirmed");
    }

    [Fact]
    public void MarkComplete_FullFlow_PreservesMenteeCompletedAt()
    {
        var session = CreateActiveSession();
        session.MarkComplete(Role.Mentee);
        var menteeTimestamp = session.MenteeCompletedAt;

        session.MarkComplete(Role.Mentor);

        // Mentee timestamp should not change when mentor confirms
        session.MenteeCompletedAt.Should().Be(menteeTimestamp);
    }

    // ── Escalation Tests ──

    [Fact]
    public void Escalate_WhenMenteeCompleted_ShouldTransitionToUnresolved()
    {
        var session = CreateActiveSession();
        session.MarkComplete(Role.Mentee);

        var result = session.Escalate();

        result.IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Unresolved);
    }

    [Fact]
    public void Escalate_WhenNotMenteeCompleted_ShouldFail()
    {
        var session = CreateActiveSession();

        var result = session.Escalate();

        result.IsFailure.Should().BeTrue();
    }

    // ── Domain Events Tests ──

    [Fact]
    public void Accept_ShouldRaiseSessionAcceptedEvent()
    {
        var session = CreatePendingAcceptanceSession();

        session.Accept();

        session.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SessionAcceptedEvent>();
    }

    [Fact]
    public void Decline_ShouldRaiseSessionDeclinedEvent()
    {
        var session = CreatePendingAcceptanceSession();

        session.Decline();

        session.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SessionDeclinedEvent>();
    }

    [Fact]
    public void MarkComplete_FullCompletion_ShouldRaiseSessionCompletedEvent()
    {
        var session = CreateActiveSession();
        session.ClearDomainEvents(); // Clear events from Accept/Activate

        session.MarkComplete(Role.Mentee);
        session.MarkComplete(Role.Mentor);

        session.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SessionCompletedEvent>();
    }

    [Fact]
    public void MarkComplete_MenteeOnly_ShouldNotRaiseCompletedEvent()
    {
        var session = CreateActiveSession();
        session.ClearDomainEvents();

        session.MarkComplete(Role.Mentee);

        session.DomainEvents.Should().BeEmpty();
    }
}
