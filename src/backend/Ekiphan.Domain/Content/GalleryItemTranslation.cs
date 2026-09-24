namespace Ekiphan.Domain.Content;

public sealed class GalleryItemTranslation
{
    private GalleryItemTranslation()
    {
    }

    public GalleryItemTranslation(
        Guid galleryItemId,
        string languageCode,
        string title,
        string? caption)
    {
        if (galleryItemId == Guid.Empty)
        {
            throw new ArgumentException("Gallery item identifier is required.", nameof(galleryItemId));
        }

        GalleryItemId = galleryItemId;
        Update(languageCode, title, caption);
    }

    public Guid GalleryItemId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Caption { get; private set; }

    internal void Update(string languageCode, string title, string? caption)
    {
        LanguageCode = languageCode.Trim().ToLowerInvariant();
        if (LanguageCode is not ("tr" or "en"))
        {
            throw new ArgumentOutOfRangeException(nameof(languageCode));
        }

        Title = Required(title, 200, nameof(title));
        Caption = string.IsNullOrWhiteSpace(caption)
            ? null
            : Required(caption, 1000, nameof(caption));
    }

    private static string Required(string value, int maximum, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        return normalized.Length <= maximum
            ? normalized
            : throw new ArgumentOutOfRangeException(name);
    }
}
