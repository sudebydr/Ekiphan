namespace Ekiphan.Api.Authentication;

internal sealed class JwtSettings
{
    public const string SectionName = "Authentication:Jwt";

    public string? Issuer { get; init; }

    public string? Audience { get; init; }

    public string? SigningKey { get; init; }

    public int AccessTokenMinutes { get; init; } = 60;

    public int IdleTimeoutMinutes { get; init; } = 30;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Issuer) &&
        !string.IsNullOrWhiteSpace(Audience) &&
        !string.IsNullOrWhiteSpace(SigningKey) &&
        SigningKey.Length >= 32 &&
        AccessTokenMinutes is >= 30 and <= 480 &&
        IdleTimeoutMinutes is >= 5 and <= 60 &&
        AccessTokenMinutes >= IdleTimeoutMinutes;
}
