using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Handles archiving an opportunity posting.
/// Sets status to Archived and removes from public visibility.
/// </summary>
public sealed class ArchiveOpportunityHandler : IRequestHandler<ArchiveOpportunityCommand, Result>
{
    private readonly IOpportunityRepository _opportunityRepository;

    public ArchiveOpportunityHandler(IOpportunityRepository opportunityRepository)
    {
        _opportunityRepository = opportunityRepository;
    }

    public async Task<Result> Handle(
        ArchiveOpportunityCommand request,
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
            return Result.Failure("You can only archive your own postings.");
        }

        var result = posting.Archive();

        if (result.IsFailure)
        {
            return result;
        }

        await _opportunityRepository.SaveAsync(posting, cancellationToken);

        return Result.Success();
    }
}
