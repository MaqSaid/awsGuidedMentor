using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Application.DTOs;

/// <summary>
/// Data transfer object for meetup event query results.
/// </summary>
public sealed record MeetupEventDto(
    Guid MeetupEventId,
    AustralianChapter Chapter,
    string Title,
    DateTime EventDate,
    string StartTime,
    string EndTime,
    string VenueName,
    string VenueAddress,
    string EventUrl,
    int ConfirmedAttendeesCount,
    bool IsCancelled)
{
    public static MeetupEventDto FromDomain(MeetupEvent entity) => new(
        MeetupEventId: entity.Id.Value,
        Chapter: entity.Chapter,
        Title: entity.Title,
        EventDate: entity.EventDate,
        StartTime: entity.StartTime.ToString("HH:mm"),
        EndTime: entity.EndTime.ToString("HH:mm"),
        VenueName: entity.VenueName,
        VenueAddress: entity.VenueAddress,
        EventUrl: entity.EventUrl,
        ConfirmedAttendeesCount: entity.ConfirmedAttendees.Count,
        IsCancelled: entity.IsCancelled);
}
