using FluentValidation;

namespace GuidedMentor.Identity.Application.Commands.Upload;

/// <summary>
/// Validates the GetUploadUrlCommand ensuring correct file format and size constraints.
/// Resume: PDF or DOCX, max 5MB.
/// ProfilePhoto: JPEG or PNG, max 2MB.
/// </summary>
public sealed class GetUploadUrlValidator : AbstractValidator<GetUploadUrlCommand>
{
    private const long MaxResumeSizeBytes = 5 * 1024 * 1024;   // 5 MB
    private const long MaxPhotoSizeBytes = 2 * 1024 * 1024;    // 2 MB

    private static readonly HashSet<string> ResumeContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private static readonly HashSet<string> PhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png"
    };

    public GetUploadUrlValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("ContentType is required.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("FileSize must be greater than zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type must be a valid UploadType.");

        RuleFor(x => x.ContentType)
            .Must((command, contentType) => IsValidContentType(command.Type, contentType))
            .WithMessage(command => command.Type == UploadType.Resume
                ? "Resume must be PDF or DOCX format."
                : "Profile photo must be JPEG or PNG format.");

        RuleFor(x => x.FileSize)
            .Must((command, fileSize) => IsWithinSizeLimit(command.Type, fileSize))
            .WithMessage(command => command.Type == UploadType.Resume
                ? "Resume file size must not exceed 5 MB."
                : "Profile photo file size must not exceed 2 MB.");
    }

    private static bool IsValidContentType(UploadType type, string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return type switch
        {
            UploadType.Resume => ResumeContentTypes.Contains(contentType),
            UploadType.ProfilePhoto => PhotoContentTypes.Contains(contentType),
            _ => false
        };
    }

    private static bool IsWithinSizeLimit(UploadType type, long fileSize)
    {
        if (fileSize <= 0)
            return false;

        return type switch
        {
            UploadType.Resume => fileSize <= MaxResumeSizeBytes,
            UploadType.ProfilePhoto => fileSize <= MaxPhotoSizeBytes,
            _ => false
        };
    }
}
