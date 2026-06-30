using GuidedMentor.SharedKernel;

namespace GuidedMentor.Identity.Application.DTOs.Onboarding;

/// <summary>
/// Step 1 (Profile) data for mentor onboarding.
/// </summary>
public sealed record MentorStep1Data(
    string FullName,
    string? ProfilePhotoUrl,
    AustralianChapter AwsChapter,
    string ProfessionalTitle,
    string CompanyName);
