using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Upload;

/// <summary>
/// Generates an S3 presigned download URL (15-minute expiry).
/// Access is validated: only the file owner or a matched mentor can download.
/// </summary>
public sealed record GetDownloadUrlQuery(
    Guid RequestingUserId,
    Guid TargetUserId,
    UploadType Type) : IRequest<Result<DownloadUrlResponse>>;

/// <summary>
/// Response containing the presigned download URL and metadata.
/// </summary>
public sealed record DownloadUrlResponse(
    string DownloadUrl,
    DateTime ExpiresAt);
