using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.ValueObjects;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Availability;

/// <summary>
/// Handles setting a mentor's availability status.
/// Updates the mentor entity and persists to the Mentors_Table.
/// </summary>
public sealed class SetMentorAvailabilityHandler : IRequestHandler<SetMentorAvailabilityCommand, Result>
{
    private readonly IMentorRepository _mentorRepository;

    public SetMentorAvailabilityHandler(IMentorRepository mentorRepository)
    {
        _mentorRepository = mentorRepository;
    }

    public async Task<Result> Handle(
        SetMentorAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var mentor = await _mentorRepository.GetByIdAsync(request.MentorId, cancellationToken);

        if (mentor is null)
        {
            return Result.Failure("Mentor not found.");
        }

        var result = request.Status switch
        {
            AvailabilityStatus.Available => mentor.SetAvailable(),
            AvailabilityStatus.Unavailable => mentor.SetUnavailable(request.Reason, request.ReturnDate),
            _ => Result.Failure("Invalid availability status.")
        };

        if (result.IsFailure)
        {
            return result;
        }

        await _mentorRepository.SaveAvailabilityAsync(mentor, cancellationToken);

        return Result.Success();
    }
}
