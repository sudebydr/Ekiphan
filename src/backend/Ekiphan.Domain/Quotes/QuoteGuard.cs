using System.Net.Mail;

namespace Ekiphan.Domain.Quotes;

internal static class QuoteGuard
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

    public static string? Optional(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Required(value, maximumLength, parameterName);
    }

    public static string Email(string value)
    {
        var normalized = Required(value, 254, nameof(value)).ToLowerInvariant();

        if (!MailAddress.TryCreate(normalized, out var address) ||
            !string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Email address is invalid.", nameof(value));
        }

        return normalized;
    }
}
