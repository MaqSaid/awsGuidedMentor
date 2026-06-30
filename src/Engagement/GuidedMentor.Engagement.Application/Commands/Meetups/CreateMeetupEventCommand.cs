using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Creates a new meetup event for a chapter. Only chapter leads can execute this.
/// </summary>
public sealed record CreateMeetupEventCommand(
    Guid ChapterLeadId,
    AustralianChapter Chapter,
    string Title,
    DateTime EventDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string VenueName,
    string VenueAddress,
    string EventUrl) : IRequest<Result<Guid>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => ChapterLeadId;
    string IAuditableCommand.AuditResourceId => $"MeetupEvent:New:Chapter:{Chapter}";
}
