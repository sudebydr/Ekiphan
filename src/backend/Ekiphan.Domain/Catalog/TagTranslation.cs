namespace Ekiphan.Domain.Catalog;

public sealed class TagTranslation
{
    private TagTranslation()
    {
    }

    internal TagTranslation(
        Guid tagId,
        string languageCode,
        string name,
        string slug)
    {
        TagId = tagId;
        LanguageCode = CatalogGuard.LanguageCode(languageCode);
        Name = CatalogGuard.Required(name, 150, nameof(name));
        Slug = CatalogGuard.Required(slug, 200, nameof(slug));
    }

    public Guid TagId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    internal void Update(string name, string slug)
    {
        Name = CatalogGuard.Required(name, 150, nameof(name));
        Slug = CatalogGuard.Required(slug, 200, nameof(slug));
    }
}
