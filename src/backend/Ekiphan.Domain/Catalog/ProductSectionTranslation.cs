namespace Ekiphan.Domain.Catalog;

public sealed class ProductSectionTranslation
{
    private ProductSectionTranslation()
    {
    }

    internal ProductSectionTranslation(
        Guid productSectionId,
        string languageCode,
        string name,
        string slug)
    {
        ProductSectionId = productSectionId;
        LanguageCode = CatalogGuard.LanguageCode(languageCode);
        Name = CatalogGuard.Required(name, 150, nameof(name));
        Slug = CatalogGuard.Required(slug, 200, nameof(slug));
    }

    public Guid ProductSectionId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    internal void Update(string name, string slug)
    {
        Name = CatalogGuard.Required(name, 150, nameof(name));
        Slug = CatalogGuard.Required(slug, 200, nameof(slug));
    }
}
