namespace Ekiphan.Domain.Media;

internal static class MediaGuard
{
    private static readonly Dictionary<string, string[]> MimeExtensions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/webp"] = [".webp"],
            ["application/pdf"] = [".pdf"],
            ["application/msword"] = [".doc"],
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] =
                [".docx"],
        };

    public static string Required(
        string value,
        int maximumLength,
        string parameterName)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static void ValidateMimeAndExtension(
        MediaAssetType assetType,
        string mimeType,
        string originalFileName)
    {
        if (!MimeExtensions.TryGetValue(mimeType, out var allowedExtensions))
        {
            throw new ArgumentException("MIME type is not allowed.", nameof(mimeType));
        }

        var expectedType = mimeType switch
        {
            "image/jpeg" or "image/png" or "image/webp" => MediaAssetType.Image,
            "application/pdf" => MediaAssetType.Pdf,
            _ => MediaAssetType.Document,
        };

        if (assetType != expectedType)
        {
            throw new ArgumentException(
                "MIME type does not match media asset type.",
                nameof(mimeType));
        }

        var extension = Path.GetExtension(originalFileName);

        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "File extension does not match MIME type.",
                nameof(originalFileName));
        }
    }

    public static string StorageKey(string value)
    {
        var normalized = Required(value, 500, nameof(value));

        if (normalized.StartsWith('/') ||
            normalized.Contains('\\') ||
            normalized.Contains("..", StringComparison.Ordinal) ||
            normalized.Any(
                character =>
                    !char.IsLetterOrDigit(character) &&
                    character is not '/' and not '-' and not '_' and not '.'))
        {
            throw new ArgumentException(
                "Storage key must be a safe relative path.",
                nameof(value));
        }

        return normalized;
    }
}
