namespace Ekiphan.Domain.Identity;

public static class AdminPasswordPolicy
{
    public const int MinimumLength = 14;
    public const int MaximumLength = 256;

    public static bool IsStrong(string? value) =>
        value is { Length: >= MinimumLength and <= MaximumLength } &&
        value.Any(char.IsUpper) &&
        value.Any(char.IsLower) &&
        value.Any(char.IsDigit) &&
        value.Any(item => !char.IsLetterOrDigit(item));

    public static void EnsureStrong(string? value)
    {
        if (!IsStrong(value))
        {
            throw new ArgumentException(
                $"Password must contain {MinimumLength}-{MaximumLength} " +
                "characters including upper, lower, digit and symbol.",
                nameof(value));
        }
    }
}

