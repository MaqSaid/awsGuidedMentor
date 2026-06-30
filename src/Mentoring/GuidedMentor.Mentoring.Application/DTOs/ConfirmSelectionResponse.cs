namespace GuidedMentor.Mentoring.Application.DTOs;

/// <summary>
/// Response returned after successfully confirming mentor selection.
/// </summary>
/// <param name="SessionId">The unique identifier of the created pending session.</param>
/// <param name="MentorId">The mentor that was selected.</param>
/// <param name="Status">The initial status of the session (PendingAcceptance).</param>
public sealed record ConfirmSelectionResponse(
    Guid SessionId,
    Guid MentorId,
    string Status);
