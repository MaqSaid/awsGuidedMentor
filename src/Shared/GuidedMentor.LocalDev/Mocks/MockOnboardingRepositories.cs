using System.Text.Json;
using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.DTOs.Onboarding;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.LocalDev.Mocks;

/// <summary>
/// In-memory onboarding progress repository for local development.
/// Stores step data in memory (resets on restart).
/// </summary>
public sealed class MockOnboardingProgressRepository : IOnboardingProgressRepository
{
    private readonly Dictionary<string, Dictionary<int, JsonDocument>> _store = new();

    private static string Key(Guid userId, Role role) => $"{userId}:{role}";

    public Task SaveStepAsync(Guid userId, Role role, int step, JsonDocument data, CancellationToken ct)
    {
        var key = Key(userId, role);
        if (!_store.ContainsKey(key))
            _store[key] = new Dictionary<int, JsonDocument>();

        _store[key][step] = data;
        return Task.CompletedTask;
    }

    public Task<Dictionary<int, JsonDocument>> GetProgressAsync(Guid userId, Role role, CancellationToken ct)
    {
        var key = Key(userId, role);
        var result = _store.TryGetValue(key, out var progress)
            ? new Dictionary<int, JsonDocument>(progress)
            : new Dictionary<int, JsonDocument>();
        return Task.FromResult(result);
    }

    public Task<int> GetLastCompletedStepAsync(Guid userId, Role role, CancellationToken ct)
    {
        var key = Key(userId, role);
        var lastStep = _store.TryGetValue(key, out var progress) && progress.Count > 0
            ? progress.Keys.Max()
            : 0;
        return Task.FromResult(lastStep);
    }
}

/// <summary>
/// No-op mentee profile repository for local development.
/// </summary>
public sealed class MockMenteeProfileRepository : IMenteeProfileRepository
{
    public Task SaveProfileAsync(
        Guid userId,
        MenteeStep1Data profile,
        MenteeStep2Data skills,
        MenteeStep3Data goals,
        MenteeStep4Data preferences,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task UpdateProfileAsync(Guid userId, MenteeSettingsData settings, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// No-op mentor profile repository for local development.
/// </summary>
public sealed class MockMentorProfileRepository : IMentorProfileRepository
{
    public Task SaveProfileAsync(
        Guid userId,
        MentorStep1Data profile,
        MentorStep2Data expertise,
        MentorStep3Data availability,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task<int> GetActiveMenteeCountAsync(Guid userId, CancellationToken ct)
    {
        return Task.FromResult(0);
    }

    public Task UpdateProfileAsync(Guid userId, MentorSettingsData settings, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
