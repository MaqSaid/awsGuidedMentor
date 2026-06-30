using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.BackgroundJobs.Commands;

/// <summary>
/// Handles the daily 90-day availability reminder check.
/// Queries mentors unavailable for >90 days and sends reminder notifications
/// asking if they wish to return or deactivate their profile.
/// </summary>
public sealed class SendMentorAvailabilityReminderHandler
    : IRequestHandler<SendMentorAvailabilityReminderCommand, Result>
{
    private readonly IMentorRepository _mentorRepository;
    private readonly IMentoringNotificationPublisher _notificationPublisher;
    private readonly ILogger<SendMentorAvailabilityReminderHandler> _logger;

    private const int UnavailableDaysThreshold = 90;

    public SendMentorAvailabilityReminderHandler(
        IMentorRepository mentorRepository,
        IMentoringNotificationPublisher notificationPublisher,
        ILogger<SendMentorAvailabilityReminderHandler> logger)
    {
        _mentorRepository = mentorRepository;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(
        SendMentorAvailabilityReminderCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Checking for mentors unavailable for more than {Days} days.",
            UnavailableDaysThreshold);

        var mentors = await _mentorRepository.GetUnavailableMentorsOverDaysAsync(
            UnavailableDaysThreshold,
            cancellationToken);

        if (mentors.Count == 0)
        {
            _logger.LogDebug("No mentors found exceeding the {Days}-day unavailability threshold.",
                UnavailableDaysThreshold);
            return Result.Success();
        }

        _logger.LogInformation(
            "Found {Count} mentor(s) unavailable for more than {Days} days. Sending reminders.",
            mentors.Count,
            UnavailableDaysThreshold);

        var sentCount = 0;
        foreach (var mentor in mentors)
        {
            try
            {
                await _notificationPublisher.SendAvailabilityReminderAsync(
                    mentor.Id,
                    mentor.DisplayName,
                    mentor.Availability.UnavailableSince!.Value,
                    cancellationToken);

                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to send availability reminder to mentor {MentorId}.",
                    mentor.Id);
            }
        }

        _logger.LogInformation(
            "Availability reminder sent to {SentCount}/{TotalCount} mentor(s).",
            sentCount, mentors.Count);

        return Result.Success();
    }
}
