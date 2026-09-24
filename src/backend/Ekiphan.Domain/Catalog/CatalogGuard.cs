namespace Ekiphan.Domain.Catalog;

internal static class CatalogGuard
{
    public static string Required(string value, int maximumLength, string parameterName)
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

    public static string LanguageCode(string value)
    {
        var normalized = Required(value, 2, nameof(value)).ToLowerInvariant();

        if (!LanguageCodes.IsSupported(normalized))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Only Turkish and English are supported.");
        }

        return normalized;
    }
}
