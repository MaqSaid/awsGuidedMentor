using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Handles the daily opportunity expiry job.
/// Archives expired postings and past-event postings.
/// Notifies mentors with renewal option for jobs only.
/// </summary>
public sealed class ProcessOpportunityExpiryHandler : IRequestHandler<ProcessOpportunityExpiryCommand, Result>
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IMentoringNotificationPublisher _notificationPublisher;

    public ProcessOpportunityExpiryHandler(
        IOpportunityRepository opportunityRepository,
        IMentoringNotificationPublisher notificationPublisher)
    {
        _opportunityRepository = opportunityRepository;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result> Handle(
        ProcessOpportunityExpiryCommand request,
        CancellationToken cancellationToken)
    {
        var expiredPostings = await _opportunityRepository.GetExpiredActivePostingsAsync(cancellationToken);

        foreach (var posting in expiredPostings)
        {
            posting.MarkExpired();
            await _opportunityRepository.SaveAsync(posting, cancellationToken);

            // Notify mentor — jobs get renewal option, others just get archived notification
            if (posting.Type == OpportunityType.Job)
            {
                await _notificationPublisher.NotifyMentorOpportunityExpiredWithRenewalAsync(
                    posting.PostedByMentorId.Value,
                    posting.Id.Value,
                    posting.Title,
                    cancellationToken);
            }
            else
            {
                await _notificationPublisher.NotifyMentorOpportunityExpiredAsync(
                    posting.PostedByMentorId.Value,
                    posting.Id.Value,
                    posting.Title,
                    cancellationToken);
            }
        }

        return Result.Success();
    }
}
