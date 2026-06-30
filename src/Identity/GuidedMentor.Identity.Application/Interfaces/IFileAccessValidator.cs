using GuidedMentor.Identity.Application.Commands.Upload;

namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Validates whether a user has permission to access another user's uploaded files.
/// Only file owners or matched mentors can download files.
/// </summary>
public interface IFileAccessValidator
{
    /// <summary>
    /// Determines if the requesting user can access files owned by the target user.
    /// Returns true if the requesting user is the file owner or a matched mentor.
    /// </summary>
    Task<bool> CanAccessFileAsync(Guid requestingUserId, Guid targetUserId, CancellationToken ct);

    /// <summary>
    /// Retrieves the S3 file key for a user's uploaded file of the specified type.
    /// Returns null if no file exists.
    /// </summary>
    Task<string?> GetFileKeyAsync(Guid userId, UploadType type, CancellationToken ct);
}
