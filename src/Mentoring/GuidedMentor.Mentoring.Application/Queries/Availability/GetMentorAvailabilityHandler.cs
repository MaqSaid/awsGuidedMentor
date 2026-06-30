using GuidedMentor.Mentoring.Application.DTOs;
using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Queries.Availability;

/// <summary>
/// Handles retrieving the current availability status for a mentor.
/// </summary>
public sealed class GetMentorAvailabilityHandler : IRequestHandler<GetMentorAvailabilityQuery, Result<MentorAvailabilityDto>>
{
    private readonly IMentorRepository _mentorRepository;

    public GetMentorAvailabilityHandler(IMentorRepository mentorRepository)
    {
        _mentorRepository = mentorRepository;
    }

    public async Task<Result<MentorAvailabilityDto>> Handle(
        GetMentorAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var availability = await _mentorRepository.GetAvailabilityAsync(request.MentorId, cancellationToken);

        if (availability is null)
        {
            return Result<MentorAvailabilityDto>.Failure("Mentor not found.");
        }

        return Result<MentorAvailabilityDto>.Success(MentorAvailabilityDto.FromDomain(availability));
    }
}
