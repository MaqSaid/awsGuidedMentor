using GuidedMentor.Mentoring.Application.Interfaces;
using GuidedMentor.Mentoring.Domain.ValueObjects;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Mentoring.Application.Commands.Opportunities;

/// <summary>
/// Handles updating a mentee's opportunity notification preferences.
/// </summary>
public sealed class UpdateOpportunityPreferencesHandler : IRequestHandler<UpdateOpportunityPreferencesCommand, Result>
{
    private readonly IMenteeRepository _menteeRepository;

    public UpdateOpportunityPreferencesHandler(IMenteeRepository menteeRepository)
    {
        _menteeRepository = menteeRepository;
    }

    public async Task<Result> Handle(
        UpdateOpportunityPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var preferences = OpportunityNotificationPreferences.Create(
            request.IsEnabled,
            request.TypePreferences,
            request.SkillMatchEnabled);

        await _menteeRepository.SaveOpportunityPreferencesAsync(
            request.MenteeId,
            preferences,
            cancellationToken);

        return Result.Success();
    }
}
