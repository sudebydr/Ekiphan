using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class ProductSection : Entity
{
    private readonly List<ProductSectionTranslation> _translations = [];

    private ProductSection()
    {
    }

    public ProductSection(Guid id, string code, int sortOrder = 0)
        : base(id)
    {
        Code = CatalogGuard.Required(code, 50, nameof(code));
        SortOrder = sortOrder;
    }

    public string Code { get; private set; } = string.Empty;

    public bool IsPublished { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<ProductSectionTranslation> Translations => _translations;

    public void SetPublished(bool isPublished) => IsPublished = isPublished;

    public void Update(string code, int sortOrder)
    {
        Code = CatalogGuard.Required(code, 50, nameof(code));
        SortOrder = sortOrder;
    }

    public void AddTranslation(string languageCode, string name, string slug)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);

        if (_translations.Any(item => item.LanguageCode == normalizedLanguage))
        {
            throw new InvalidOperationException(
                $"Translation '{normalizedLanguage}' already exists.");
        }

        _translations.Add(
            new ProductSectionTranslation(Id, normalizedLanguage, name, slug));
    }

    public void SetTranslation(string languageCode, string name, string slug)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == normalizedLanguage);
        if (translation is null)
        {
            AddTranslation(normalizedLanguage, name, slug);
            return;
        }

        translation.Update(name, slug);
    }

    public void RemoveTranslation(string languageCode)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == normalizedLanguage);
        if (translation is null)
        {
            return;
        }

        if (_translations.Count == 1)
        {
            throw new InvalidOperationException(
                "A product section must keep at least one translation.");
        }

        _translations.Remove(translation);
    }
}
