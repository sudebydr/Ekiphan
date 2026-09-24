namespace Ekiphan.Domain.Catalog;

public sealed class AttributeOptionTranslation
{
    private AttributeOptionTranslation()
    {
    }

    internal AttributeOptionTranslation(
        Guid attributeOptionId,
        string languageCode,
        string name)
    {
        AttributeOptionId = attributeOptionId;
        LanguageCode = CatalogGuard.LanguageCode(languageCode);
        Name = CatalogGuard.Required(name, 150, nameof(name));
    }

    public Guid AttributeOptionId { get; private set; }

    public string LanguageCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    internal void Update(string name) =>
        Name = CatalogGuard.Required(name, 150, nameof(name));
}
