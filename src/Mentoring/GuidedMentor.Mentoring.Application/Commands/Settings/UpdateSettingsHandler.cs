using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Settings;

/// <summary>
/// Handles mentor settings updates.
/// Enforces that maxMentees cannot be reduced below the current active mentee count (Req 13.6).
/// Flags the mentor for compatibility score recalculation when chapter changes (Req 13.7).
/// Validates all inputs using the same onboarding rules via FluentValidation pipeline (Req 13.2).
/// </summary>
public sealed class UpdateSettingsHandler : IRequestHandler<UpdateSettingsCommand, Result>
{
    private readonly IMentorRepository _mentorRepository;

    public UpdateSettingsHandler(IMentorRepository mentorRepository)
    {
        _mentorRepository = mentorRepository;
    }

    public async Task<Result> Handle(
        UpdateSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var mentor = await _mentorRepository.GetByIdAsync(request.MentorId, cancellationToken);

        if (mentor is null)
        {
            return Result.Failure("Mentor not found.");
        }

        // Enforce maxMentees constraint: cannot reduce below active mentee count (Req 13.6)
        var maxMenteesResult = mentor.UpdateMaxMentees(request.MaxMentees);
        if (maxMenteesResult.IsFailure)
        {
            return maxMenteesResult;
        }

        // Flag for compatibility score recalculation on chapter change (Req 13.7)
        mentor.UpdateChapter(request.Chapter);

        // Persist all updated settings
        await _mentorRepository.SaveSettingsAsync(mentor, cancellationToken);

        return Result.Success();
    }
}
