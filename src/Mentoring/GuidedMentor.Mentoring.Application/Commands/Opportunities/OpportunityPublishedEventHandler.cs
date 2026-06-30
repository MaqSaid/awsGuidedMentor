using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Handles the OpportunityPublishedEvent domain event.
/// Notifies:
/// 1. All mentees with active sessions with this mentor.
/// 2. Mentees who opted into skill-match notifications with ≥2 skill overlap.
/// </summary>
public sealed class OpportunityPublishedEventHandler : INotificationHandler<OpportunityPublishedEvent>
{
    private readonly IMenteeRepository _menteeRepository;
    private readonly IMentoringNotificationPublisher _notificationPublisher;

    public OpportunityPublishedEventHandler(
        IMenteeRepository menteeRepository,
        IMentoringNotificationPublisher notificationPublisher)
    {
        _menteeRepository = menteeRepository;
        _notificationPublisher = notificationPublisher;
    }

    public async Task Handle(OpportunityPublishedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Notify all mentees with active sessions with this mentor
        var matchedMenteeIds = await _menteeRepository.GetMenteeIdsWithActiveSessionsForMentorAsync(
            notification.MentorId.Value,
            cancellationToken);

        foreach (var menteeId in matchedMenteeIds)
        {
            await _notificationPublisher.NotifyMenteeOfNewOpportunityAsync(
                menteeId,
                notification.PostingId.Value,
                notification.MentorId.Value,
                notification.Type,
                cancellationToken);
        }

        // 2. Notify skill-matched opt-in mentees (≥2 skill overlap)
        if (notification.RequiredSkills.Count > 0)
        {
            var skillMatchedMenteeIds = await _menteeRepository.GetSkillMatchedMenteeIdsAsync(
                notification.RequiredSkills,
                minimumOverlap: 2,
                cancellationToken);

            // Avoid duplicate notifications for mentees already notified above
            var alreadyNotified = new HashSet<Guid>(matchedMenteeIds);

            foreach (var menteeId in skillMatchedMenteeIds)
            {
                if (alreadyNotified.Contains(menteeId))
                    continue;

                // Check individual mentee preferences for this opportunity type
                var preferences = await _menteeRepository.GetOpportunityPreferencesAsync(menteeId, cancellationToken);
                if (preferences is null || !preferences.ShouldNotifyForType(notification.Type))
                    continue;

                await _notificationPublisher.NotifyMenteeOfSkillMatchedOpportunityAsync(
                    menteeId,
                    notification.PostingId.Value,
                    notification.MentorId.Value,
                    notification.Type,
                    cancellationToken);
            }
        }
    }
}
