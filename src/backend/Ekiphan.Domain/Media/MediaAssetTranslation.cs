namespace Ekiphan.Domain.Media;

public sealed class MediaAssetTranslation
{
    private MediaAssetTranslation()
    {
    }

    internal MediaAssetTranslation(
        Guid mediaAssetId,
        string languageCode,
        string title,
        string? altText,
        string? description)
    {
        MediaAssetId = mediaAssetId;
        LanguageCode = languageCode;
        Title = MediaGuard.Required(title, 250, nameof(title));
        AltText = altText?.Trim();
        Description = NormalizeDescription(description);
    }

    public Guid MediaAssetId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? AltText { get; private set; }

    public string? Description { get; private set; }

    internal void Update(string title, string? altText, string? description)
    {
        Title = MediaGuard.Required(title, 250, nameof(title));
        AltText = altText?.Trim();
        Description = NormalizeDescription(description);
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description)
            ? null
            : MediaGuard.Required(description, 2000, nameof(description));
}
