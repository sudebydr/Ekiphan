using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class GalleryItem : Entity
{
    private readonly List<GalleryItemTranslation> _translations = [];

    private GalleryItem()
    {
    }

    public GalleryItem(Guid id, Guid mediaAssetId, int sortOrder = 0) : base(id)
    {
        SetMedia(mediaAssetId);
        SortOrder = sortOrder;
    }

    public Guid MediaAssetId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPublished { get; private set; }
    public IReadOnlyCollection<GalleryItemTranslation> Translations => _translations;

    public void Configure(Guid mediaAssetId, int sortOrder)
    {
        SetMedia(mediaAssetId);
        SortOrder = sortOrder;
    }

    public void SetTranslation(string languageCode, string title, string? caption)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        var translation = _translations.SingleOrDefault(item => item.LanguageCode == language);
        if (translation is null)
        {
            _translations.Add(new GalleryItemTranslation(Id, language, title, caption));
            return;
        }

        translation.Update(language, title, caption);
    }

    public void RemoveTranslation(string languageCode)
    {
        var language = languageCode.Trim().ToLowerInvariant();
        var translation = _translations.SingleOrDefault(item => item.LanguageCode == language);
        if (translation is not null && _translations.Count > 1)
        {
            _translations.Remove(translation);
        }
    }

    public void SetPublished(bool value)
    {
        if (value && _translations.Count == 0)
        {
            throw new InvalidOperationException(
                "Gallery item requires a translation before publishing.");
        }

        IsPublished = value;
    }

    private void SetMedia(Guid mediaAssetId)
    {
        if (mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException("Media asset identifier is required.", nameof(mediaAssetId));
        }

        MediaAssetId = mediaAssetId;
    }
}
