using GuidedMentor.SharedKernel;
using MediatR;

namespace GuidedMentor.Identity.Application.Commands.Upload;

/// <summary>
/// Generates an S3 presigned upload URL for a resume (PDF/DOCX ≤5MB) or profile photo (JPEG/PNG ≤2MB).
/// The URL expires after 5 minutes.
/// </summary>
public sealed record GetUploadUrlCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long FileSize,
    UploadType Type) : IRequest<Result<UploadUrlResponse>>, IAuditableCommand
{
    Guid IAuditableCommand.UserId => UserId;
    string IAuditableCommand.AuditResourceId => $"User:{UserId}:Upload:{Type}";
}

/// <summary>
/// Response containing the presigned upload URL and metadata.
/// </summary>
public sealed record UploadUrlResponse(
    string UploadUrl,
    DateTime ExpiresAt,
    string FileKey);
