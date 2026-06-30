using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Upload;

/// <summary>
/// Handles GetDownloadUrlQuery by validating access (owner or matched mentor)
/// and generating an S3 presigned download URL (15-minute expiry).
/// </summary>
public sealed class GetDownloadUrlHandler : IRequestHandler<GetDownloadUrlQuery, Result<DownloadUrlResponse>>
{
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromMinutes(15);

    private readonly IS3UploadService _s3UploadService;
    private readonly IFileAccessValidator _fileAccessValidator;

    public GetDownloadUrlHandler(
        IS3UploadService s3UploadService,
        IFileAccessValidator fileAccessValidator)
    {
        _s3UploadService = s3UploadService;
        _fileAccessValidator = fileAccessValidator;
    }

    public async Task<Result<DownloadUrlResponse>> Handle(
        GetDownloadUrlQuery request,
        CancellationToken cancellationToken)
    {
        // Validate access: only file owner or matched mentor can download
        var hasAccess = await _fileAccessValidator.CanAccessFileAsync(
            request.RequestingUserId,
            request.TargetUserId,
            cancellationToken);

        if (!hasAccess)
        {
            return Result<DownloadUrlResponse>.Failure(
                "You do not have permission to download this file.");
        }

        // Resolve the file key for the target user
        var fileKey = await _fileAccessValidator.GetFileKeyAsync(
            request.TargetUserId,
            request.Type,
            cancellationToken);

        if (string.IsNullOrEmpty(fileKey))
        {
            return Result<DownloadUrlResponse>.Failure(
                "No file found for the specified user and type.");
        }

        var downloadUrl = await _s3UploadService.GenerateDownloadUrlAsync(
            fileKey,
            DownloadUrlExpiry,
            cancellationToken);

        var expiresAt = DateTime.UtcNow.Add(DownloadUrlExpiry);

        return Result<DownloadUrlResponse>.Success(
            new DownloadUrlResponse(downloadUrl, expiresAt));
    }
}
