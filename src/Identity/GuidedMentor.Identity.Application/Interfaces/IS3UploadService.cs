namespace GuidedMentor.Identity.Application.Interfaces;

/// <summary>
/// Abstraction over Amazon S3 presigned URL generation.
/// Implementation lives in the Infrastructure layer.
/// </summary>
public interface IS3UploadService
{
    /// <summary>
    /// Generates a presigned URL for uploading a file to S3.
    /// </summary>
    /// <param name="key">The S3 object key (path).</param>
    /// <param name="contentType">The expected content type of the upload.</param>
    /// <param name="expiry">How long the URL remains valid.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The presigned upload URL.</returns>
    Task<string> GenerateUploadUrlAsync(string key, string contentType, TimeSpan expiry, CancellationToken ct);

    /// <summary>
    /// Generates a presigned URL for downloading a file from S3.
    /// </summary>
    /// <param name="key">The S3 object key (path).</param>
    /// <param name="expiry">How long the URL remains valid.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The presigned download URL.</returns>
    Task<string> GenerateDownloadUrlAsync(string key, TimeSpan expiry, CancellationToken ct);
}
