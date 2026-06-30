using GuidedMentor.SharedKernel;

namespace GuidedMentor.Engagement.Application.Interfaces;

/// <summary>
/// Validates whether a user has the chapter_lead flag for their chapter.
/// Cross-context call to the Identity bounded context.
/// </summary>
public interface IChapterLeadValidator
{
    /// <summary>
    /// Returns true if the specified user is a chapter lead for the given chapter.
    /// </summary>
    Task<bool> IsChapterLeadAsync(
        Guid userId,
        AustralianChapter chapter,
        CancellationToken cancellationToken = default);
}
