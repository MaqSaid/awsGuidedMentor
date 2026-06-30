using GuidedMentor.Engagement.Domain.Events;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Domain.Entities;

/// <summary>
/// Represents an AWS User Group meetup event in the calendar.
/// Only chapter leads can create and cancel meetup events.
/// Mentors can confirm/withdraw attendance.
/// </summary>
public sealed class MeetupEvent : AggregateRoot<MeetupEventId>
{
    private readonly List<MentorId> _confirmedAttendees = [];

    public AustralianChapter Chapter { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTime EventDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string VenueName { get; private set; } = string.Empty;
    public string VenueAddress { get; private set; } = string.Empty;
    public string EventUrl { get; private set; } = string.Empty;
    public UserId CreatedBy { get; private set; } = default!;
    public bool IsCancelled { get; private set; }
    public IReadOnlyList<MentorId> ConfirmedAttendees => _confirmedAttendees.AsReadOnly();
    public DateTime CreatedAt { get; private set; }

    private MeetupEvent() { }

    /// <summary>
    /// Creates a new meetup event. Only chapter leads should call this.
    /// </summary>
    public static MeetupEvent Create(
        UserId createdBy,
        AustralianChapter chapter,
        string title,
        DateTime eventDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string venueName,
        string venueAddress,
        string eventUrl)
    {
        var meetup = new MeetupEvent
        {
            Id = MeetupEventId.New(),
            CreatedBy = createdBy,
            Chapter = chapter,
            Title = title,
            EventDate = eventDate,
            StartTime = startTime,
            EndTime = endTime,
            VenueName = venueName,
            VenueAddress = venueAddress,
            EventUrl = eventUrl,
            IsCancelled = false,
            CreatedAt = DateTime.UtcNow
        };

        return meetup;
    }

    /// <summary>
    /// Reconstitutes a MeetupEvent from persistence (DynamoDB).
    /// </summary>
    public static MeetupEvent Reconstitute(
        MeetupEventId id,
        UserId createdBy,
        AustralianChapter chapter,
        string title,
        DateTime eventDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string venueName,
        string venueAddress,
        string eventUrl,
        bool isCancelled,
        List<MentorId> confirmedAttendees,
        DateTime createdAt)
    {
        var meetup = new MeetupEvent
        {
            Id = id,
            CreatedBy = createdBy,
            Chapter = chapter,
            Title = title,
            EventDate = eventDate,
            StartTime = startTime,
            EndTime = endTime,
            VenueName = venueName,
            VenueAddress = venueAddress,
            EventUrl = eventUrl,
            IsCancelled = isCancelled,
            CreatedAt = createdAt
        };
        meetup._confirmedAttendees.AddRange(confirmedAttendees);
        return meetup;
    }

    /// <summary>
    /// Cancels the meetup event. Only the chapter lead who created it can cancel.
    /// </summary>
    public Result Cancel(UserId requestedBy)
    {
        if (requestedBy != CreatedBy)
            return Result.Failure("Only the chapter lead who created this event can cancel it.");

        if (IsCancelled)
            return Result.Failure("Event is already cancelled.");

        IsCancelled = true;

        RaiseDomainEvent(new MeetupEventCancelledEvent(Id, Chapter, Title, EventDate));

        return Result.Success();
    }

    /// <summary>
    /// Confirms a mentor's attendance at this meetup event.
    /// </summary>
    public Result ConfirmAttendance(MentorId mentorId)
    {
        if (IsCancelled)
            return Result.Failure("Cannot confirm attendance for a cancelled event.");

        if (EventDate.Date < DateTime.UtcNow.Date)
            return Result.Failure("Cannot confirm attendance for a past event.");

        if (_confirmedAttendees.Contains(mentorId))
            return Result.Failure("Mentor has already confirmed attendance.");

        _confirmedAttendees.Add(mentorId);

        RaiseDomainEvent(new MeetupAttendanceConfirmedEvent(Id, mentorId));

        return Result.Success();
    }

    /// <summary>
    /// Withdraws a mentor's attendance from this meetup event.
    /// </summary>
    public Result WithdrawAttendance(MentorId mentorId)
    {
        if (IsCancelled)
            return Result.Failure("Cannot withdraw attendance from a cancelled event.");

        if (EventDate.Date < DateTime.UtcNow.Date)
            return Result.Failure("Cannot withdraw attendance from a past event.");

        if (!_confirmedAttendees.Contains(mentorId))
            return Result.Failure("Mentor has not confirmed attendance.");

        _confirmedAttendees.Remove(mentorId);

        return Result.Success();
    }
}
