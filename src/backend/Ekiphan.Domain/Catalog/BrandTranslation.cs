namespace Ekiphan.Domain.Catalog;

public sealed class BrandTranslation
{
    private BrandTranslation()
    {
    }

    internal BrandTranslation(
        Guid brandId,
        string languageCode,
        string description,
        string slug)
    {
        BrandId = brandId;
        LanguageCode = CatalogGuard.LanguageCode(languageCode);
        Description = CatalogGuard.Required(description, 4000, nameof(description));
        Slug = CatalogGuard.Required(slug, 200, nameof(slug));
    }

    public Guid BrandId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    internal void Update(string description, string slug)
    {
        Description = CatalogGuard.Required(
            description,
            4000,
            nameof(description));
        Slug = CatalogGuard.Required(slug, 200, nameof(slug));
    }
}
