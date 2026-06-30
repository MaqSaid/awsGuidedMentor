using GuidedMentor.Engagement.Application.Interfaces;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Engagement.Application.Commands.Meetups;

/// <summary>
/// Handles sending 24-hour reminder notifications for meetup-aligned sessions.
/// Finds meetup events happening tomorrow, identifies aligned sessions,
/// and sends reminders with venue details and session agenda summary.
/// </summary>
public sealed class SendMeetupSessionRemindersHandler
    : IRequestHandler<SendMeetupSessionRemindersCommand, Result<int>>
{
    private readonly IMeetupEventRepository _meetupRepository;
    private readonly IMeetupNotificationPublisher _notificationPublisher;
    private readonly ISessionInfoProvider _sessionInfoProvider;

    public SendMeetupSessionRemindersHandler(
        IMeetupEventRepository meetupRepository,
        IMeetupNotificationPublisher notificationPublisher,
        ISessionInfoProvider sessionInfoProvider)
    {
        _meetupRepository = meetupRepository;
        _notificationPublisher = notificationPublisher;
        _sessionInfoProvider = sessionInfoProvider;
    }

    public async Task<Result<int>> Handle(
        SendMeetupSessionRemindersCommand request,
        CancellationToken cancellationToken)
    {
        // Get upcoming meetups for the chapter (tomorrow = within 24 hours)
        var meetups = await _meetupRepository.GetUpcomingByChapterAsync(
            request.Chapter, limit: 10, cancellationToken);

        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var tomorrowMeetups = meetups
            .Where(m => m.EventDate.Date == tomorrow && !m.IsCancelled)
            .ToList();

        var remindersSent = 0;

        foreach (var meetup in tomorrowMeetups)
        {
            var alignedSessionIds = await _meetupRepository.GetAlignedSessionIdsAsync(
                meetup.Id, cancellationToken);

            foreach (var sessionId in alignedSessionIds)
            {
                var sessionInfo = await _sessionInfoProvider.GetSessionInfoAsync(
                    sessionId, cancellationToken);

                if (sessionInfo is null)
                    continue;

                await _notificationPublisher.SendMeetupSessionReminderAsync(
                    sessionId: sessionId,
                    mentorId: sessionInfo.MentorId,
                    menteeId: sessionInfo.MenteeId,
                    meetupEventId: meetup.Id.Value,
                    venueName: meetup.VenueName,
                    venueAddress: meetup.VenueAddress,
                    sessionTitle: sessionInfo.Title,
                    cancellationToken: cancellationToken);

                remindersSent++;
            }
        }

        return Result<int>.Success(remindersSent);
    }
}
