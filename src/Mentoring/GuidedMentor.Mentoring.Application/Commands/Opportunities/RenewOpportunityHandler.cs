using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Handles renewing an expired job posting.
/// Extends expiry by 30 days. Only jobs can be renewed.
/// </summary>
public sealed class RenewOpportunityHandler : IRequestHandler<RenewOpportunityCommand, Result>
{
    private readonly IOpportunityRepository _opportunityRepository;

    public RenewOpportunityHandler(IOpportunityRepository opportunityRepository)
    {
        _opportunityRepository = opportunityRepository;
    }

    public async Task<Result> Handle(
        RenewOpportunityCommand request,
        CancellationToken cancellationToken)
    {
        var postingId = new OpportunityPostingId(request.PostingId);
        var posting = await _opportunityRepository.GetByIdAsync(postingId, cancellationToken);

        if (posting is null)
        {
            return Result.Failure("Opportunity posting not found.");
        }

        if (posting.PostedByMentorId.Value != request.MentorId)
        {
            return Result.Failure("You can only renew your own postings.");
        }

        var result = posting.Renew();

        if (result.IsFailure)
        {
            return result;
        }

        await _opportunityRepository.SaveAsync(posting, cancellationToken);

        return Result.Success();
    }
}
