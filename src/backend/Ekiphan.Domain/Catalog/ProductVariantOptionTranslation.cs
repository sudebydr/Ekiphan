namespace Ekiphan.Domain.Catalog;

public sealed class ProductVariantOptionTranslation
{
    private ProductVariantOptionTranslation()
    {
    }

    internal ProductVariantOptionTranslation(
        Guid variantOptionId,
        string languageCode,
        string name)
    {
        VariantOptionId = variantOptionId;
        LanguageCode = CatalogGuard.LanguageCode(languageCode);
        Name = CatalogGuard.Required(name, 150, nameof(name));
    }

    public Guid VariantOptionId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    internal void Update(string name) =>
        Name = CatalogGuard.Required(name, 150, nameof(name));
}
