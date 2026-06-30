using System.Text.Json;
using GuidedMentor.Identity.Application.DTOs;
using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Settings;

/// <summary>
/// Handles updating user settings/profile for the currently active role.
/// Validates all inputs using the same rules as onboarding (via FluentValidation pipeline).
/// For mentors: enforces maxMentees ≥ current activeMenteeCount.
/// On chapter change: raises ChapterChangedEvent for compatibility score recalculation.
/// </summary>
public sealed class UpdateSettingsHandler
    : IRequestHandler<UpdateSettingsCommand, Result<UpdateSettingsResponse>>
{
    private readonly GuidedMentor.Identity.Domain.Repositories.IUserRepository _userRepository;
    private readonly IMentorProfileRepository _mentorProfileRepository;
    private readonly IMenteeProfileRepository _menteeProfileRepository;

    public UpdateSettingsHandler(
        GuidedMentor.Identity.Domain.Repositories.IUserRepository userRepository,
        IMentorProfileRepository mentorProfileRepository,
        IMenteeProfileRepository menteeProfileRepository)
    {
        _userRepository = userRepository;
        _mentorProfileRepository = mentorProfileRepository;
        _menteeProfileRepository = menteeProfileRepository;
    }

    public async Task<Result<UpdateSettingsResponse>> Handle(
        UpdateSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(
            new UserId(request.UserId), cancellationToken);

        if (user is null)
        {
            return Result<UpdateSettingsResponse>.Failure("User not found.");
        }

        if (user.ActiveRole != request.Role)
        {
            return Result<UpdateSettingsResponse>.Failure(
                "Settings can only be updated for the currently active role.");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var chapterChanged = false;

        switch (request.Role)
        {
            case Role.Mentor:
                var mentorSettings = JsonSerializer.Deserialize<MentorSettingsData>(
                    request.Data.RootElement.GetRawText(), options);

                if (mentorSettings is null)
                {
                    return Result<UpdateSettingsResponse>.Failure(
                        "Invalid mentor settings data.");
                }

                // Enforce maxMentees constraint: new value must be ≥ current activeMenteeCount
                var activeMenteeCount = await _mentorProfileRepository.GetActiveMenteeCountAsync(
                    request.UserId, cancellationToken);

                if (mentorSettings.MaxMentees < activeMenteeCount)
                {
                    return Result<UpdateSettingsResponse>.Failure(
                        $"Maximum mentees cannot be less than your current active mentee count ({activeMenteeCount}). " +
                        $"Complete or release existing sessions before reducing capacity.");
                }

                // Check for chapter change before persisting
                chapterChanged = user.AwsChapter != mentorSettings.AwsChapter;

                // Update the User aggregate (chapter, display name, city, photo)
                user.UpdateProfile(
                    mentorSettings.FullName,
                    mentorSettings.City,
                    mentorSettings.ProfilePhotoUrl);

                if (chapterChanged)
                {
                    user.UpdateChapter(mentorSettings.AwsChapter);
                }

                await _userRepository.SaveAsync(user, cancellationToken);

                // Update the mentor profile in Mentors_Table
                await _mentorProfileRepository.UpdateProfileAsync(
                    request.UserId, mentorSettings, cancellationToken);

                break;

            case Role.Mentee:
                var menteeSettings = JsonSerializer.Deserialize<MenteeSettingsData>(
                    request.Data.RootElement.GetRawText(), options);

                if (menteeSettings is null)
                {
                    return Result<UpdateSettingsResponse>.Failure(
                        "Invalid mentee settings data.");
                }

                // Check for chapter change before persisting
                chapterChanged = user.AwsChapter != menteeSettings.AwsChapter;

                // Update the User aggregate (chapter, display name, city, photo)
                user.UpdateProfile(
                    menteeSettings.FullName,
                    menteeSettings.City,
                    menteeSettings.ProfilePhotoUrl);

                if (chapterChanged)
                {
                    user.UpdateChapter(menteeSettings.AwsChapter);
                }

                await _userRepository.SaveAsync(user, cancellationToken);

                // Update the mentee profile in Mentees_Table
                await _menteeProfileRepository.UpdateProfileAsync(
                    request.UserId, menteeSettings, cancellationToken);

                break;

            default:
                return Result<UpdateSettingsResponse>.Failure(
                    "Invalid role specified.");
        }

        var message = chapterChanged
            ? "Settings updated successfully. Compatibility scores will be recalculated on your next browse."
            : "Settings updated successfully.";

        return Result<UpdateSettingsResponse>.Success(
            new UpdateSettingsResponse(chapterChanged, message));
    }
}
