using GuidedMentor.Identity.Application.Interfaces;
using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Upload;

/// <summary>
/// Handles GetUploadUrlCommand by generating an S3 presigned upload URL (5-minute expiry).
/// Uses a sanitized key pattern: {type}/{userId}/{timestamp}_{sanitized_filename}
/// </summary>
public sealed class GetUploadUrlHandler : IRequestHandler<GetUploadUrlCommand, Result<UploadUrlResponse>>
{
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(5);

    private readonly IS3UploadService _s3UploadService;

    public GetUploadUrlHandler(IS3UploadService s3UploadService)
    {
        _s3UploadService = s3UploadService;
    }

    public async Task<Result<UploadUrlResponse>> Handle(
        GetUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        var sanitizedFileName = FileNameSanitizer.Sanitize(request.FileName);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var typeFolder = request.Type.ToString().ToLowerInvariant();
        var fileKey = $"{typeFolder}/{request.UserId}/{timestamp}_{sanitizedFileName}";

        var uploadUrl = await _s3UploadService.GenerateUploadUrlAsync(
            fileKey,
            request.ContentType,
            UploadUrlExpiry,
            cancellationToken);

        var expiresAt = DateTime.UtcNow.Add(UploadUrlExpiry);

        return Result<UploadUrlResponse>.Success(
            new UploadUrlResponse(uploadUrl, expiresAt, fileKey));
    }
}
