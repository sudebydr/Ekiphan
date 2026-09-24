namespace Ekiphan.Domain.Catalog;

public sealed class ProductVariantGroupTranslation
{
    private ProductVariantGroupTranslation()
    {
    }

    internal ProductVariantGroupTranslation(
        Guid variantGroupId,
        string languageCode,
        string name)
    {
        VariantGroupId = variantGroupId;
        LanguageCode = CatalogGuard.LanguageCode(languageCode);
        Name = CatalogGuard.Required(name, 150, nameof(name));
    }

    public Guid VariantGroupId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    internal void Update(string name) =>
        Name = CatalogGuard.Required(name, 150, nameof(name));
}
