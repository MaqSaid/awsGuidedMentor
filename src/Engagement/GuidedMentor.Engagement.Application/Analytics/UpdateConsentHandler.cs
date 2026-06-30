using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Domain.Repositories;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Application.Analytics;

/// <summary>
/// Handles updating a user's tracking consent preference.
/// Persists the preference so the backend can also respect opt-out.
///
/// Requirements: 30.7, 30.8
/// </summary>
public sealed class UpdateConsentHandler
{
    private readonly IConsentRepository _repository;

    public UpdateConsentHandler(IConsentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result> HandleAsync(UpdateConsentCommand command, CancellationToken ct = default)
    {
        if (command.Consent is not ("granted" or "denied"))
            return Result.Failure("Consent must be 'granted' or 'denied'.");

        var existing = await _repository.GetByUserIdAsync(command.UserId, ct);

        if (existing is not null)
        {
            existing.UpdateStatus(command.Consent);
            await _repository.UpsertAsync(existing, ct);
        }
        else
        {
            var preference = ConsentPreference.Create(command.UserId, command.Consent);
            await _repository.UpsertAsync(preference, ct);
        }

        return Result.Success();
    }
}
