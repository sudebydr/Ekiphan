using System.Text.Json;

namespace Ekiphan.Domain.DataImport;

internal static class ImportGuard
{
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

    public static string Json(string value, int maximumLength, string parameterName)
    {
        var normalized = Required(value, maximumLength, parameterName);

        try
        {
            using var document = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Value must contain valid JSON.",
                parameterName,
                exception);
        }

        return normalized;
    }

    public static string Sha256(string value, string parameterName)
    {
        var normalized = Required(value, 64, parameterName).ToUpperInvariant();

        if (normalized.Length != 64 ||
            normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Value must be a 64-character SHA-256 hex string.",
                parameterName);
        }

        return normalized;
    }
}
