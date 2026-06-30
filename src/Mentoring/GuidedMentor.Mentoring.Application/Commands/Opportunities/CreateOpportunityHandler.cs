using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Domain.Repositories;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Handles creating a new opportunity posting.
/// Enforces the max 5 active postings per mentor rule.
/// </summary>
public sealed class CreateOpportunityHandler : IRequestHandler<CreateOpportunityCommand, Result<Guid>>
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IMediator _mediator;
    private const int MaxActivePostings = 5;

    public CreateOpportunityHandler(
        IOpportunityRepository opportunityRepository,
        IMediator mediator)
    {
        _opportunityRepository = opportunityRepository;
        _mediator = mediator;
    }

    public async Task<Result<Guid>> Handle(
        CreateOpportunityCommand request,
        CancellationToken cancellationToken)
    {
        var mentorId = new MentorId(request.MentorId);

        // Enforce max 5 active postings per mentor (all types combined)
        var activeCount = await _opportunityRepository.GetActiveCountByMentorAsync(mentorId, cancellationToken);

        if (activeCount >= MaxActivePostings)
        {
            return Result<Guid>.Failure(
                "Maximum active posting limit reached (5). Please archive an existing posting before creating a new one.");
        }

        var posting = OpportunityPosting.Create(
            mentorId: mentorId,
            title: request.Title,
            type: request.Type,
            organisationName: request.OrganisationName,
            description: request.Description,
            location: request.Location,
            eventDateTime: request.EventDateTime,
            employmentType: request.EmploymentType,
            requiredSkills: request.RequiredSkills,
            requiredExperience: request.RequiredExperience,
            externalUrl: request.ExternalUrl);

        await _opportunityRepository.SaveAsync(posting, cancellationToken);

        // Publish domain events (OpportunityPublishedEvent)
        foreach (var domainEvent in posting.DomainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        posting.ClearDomainEvents();

        return Result<Guid>.Success(posting.Id.Value);
    }
}
