using System.Text.RegularExpressions;

namespace GuidedMentor.Identity.Application.Commands.Upload;

/// <summary>
/// Sanitizes file names for safe S3 storage.
/// Strips all characters except alphanumeric, hyphens, and underscores.
/// Preserves file extension and lowercases the result.
/// </summary>
public static partial class FileNameSanitizer
{
    [GeneratedRegex("[^a-z0-9\\-_]")]
    private static partial Regex InvalidCharsRegex();

    /// <summary>
    /// Sanitizes a filename by removing unsafe characters, preserving the extension, and lowercasing.
    /// </summary>
    public static string Sanitize(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();

        var sanitized = InvalidCharsRegex().Replace(nameWithoutExtension, string.Empty);

        if (string.IsNullOrEmpty(sanitized))
            sanitized = "file";

        return $"{sanitized}{extension}";
    }
}
