using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

namespace Ekiphan.Domain.Common;

public static partial class SeoValue
{
    public static string GenerateSlug(string value, int maximumLength = 300)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var source = value.Trim().ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ş', 's');
        var normalized = source.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var pendingHyphen = false;
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (pendingHyphen && builder.Length > 0) builder.Append('-');
                builder.Append(character);
                pendingHyphen = false;
            }
            else
            {
                pendingHyphen = true;
            }

            if (builder.Length >= maximumLength) break;
        }

        return Slug(builder.ToString().TrimEnd('-'), maximumLength);
    }

    public static string Slug(string value, int maximumLength = 300)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > maximumLength || !SlugPattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Slug must contain lowercase ASCII letters, digits and single hyphens.",
                nameof(value));
        }

        return normalized;
    }

    public static string? Optional(
        string? value,
        int maximumLength,
        string parameterName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static string? Canonical(string? value)
    {
        var normalized = Optional(value, 2048, nameof(value));
        if (normalized is null) return null;
        if (normalized.StartsWith('/') &&
            !normalized.StartsWith("//", StringComparison.Ordinal))
        {
            return normalized;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException(
                "Canonical URL must be site-relative or absolute HTTPS.",
                nameof(value));
        }

        return uri.AbsoluteUri;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
