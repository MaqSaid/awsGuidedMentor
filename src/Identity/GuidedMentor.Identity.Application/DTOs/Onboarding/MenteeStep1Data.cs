using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.DTOs.Onboarding;

/// <summary>
/// Step 1 (Profile) data for mentee onboarding.
/// </summary>
public sealed record MenteeStep1Data(
    string FullName,
    string? ProfilePhotoUrl,
    AustralianChapter AwsChapter,
    string City);
