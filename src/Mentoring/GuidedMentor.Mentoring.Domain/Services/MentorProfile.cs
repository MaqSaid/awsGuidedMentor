using GuidedMentor.Mentoring.Domain.ValueObjects;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Services;

/// <summary>
/// Represents a mentor's profile data used as input for compatibility scoring.
/// </summary>
public sealed record MentorProfile(
    string DisplayName,
    AustralianChapter Chapter,
    string City,
    IReadOnlyList<string> ExpertiseAreas,
    IReadOnlyList<string> Topics,
    int YearsOfExperience,
    int MaxMentees,
    int ActiveMenteeCount,
    AvailabilityStatus AvailabilityStatus = AvailabilityStatus.Available);
