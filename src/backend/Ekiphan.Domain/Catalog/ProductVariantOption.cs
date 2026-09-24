using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class ProductVariantOption : Entity
{
    private readonly List<ProductVariantOptionTranslation> _translations = [];

    private ProductVariantOption()
    {
    }

    internal ProductVariantOption(
        Guid id,
        Guid variantGroupId,
        string code,
        int sortOrder)
        : base(id)
    {
        VariantGroupId = variantGroupId;
        Code = CatalogGuard.Required(code, 100, nameof(code)).ToUpperInvariant();
        SortOrder = sortOrder;
    }

    public Guid VariantGroupId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<ProductVariantOptionTranslation> Translations =>
        _translations;

    public void SetActive(bool isActive) => IsActive = isActive;

    public void Update(string code, int sortOrder)
    {
        Code = CatalogGuard.Required(code, 100, nameof(code)).ToUpperInvariant();
        SortOrder = sortOrder;
    }

    public void AddTranslation(string languageCode, string name)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);

        if (_translations.Any(item => item.LanguageCode == normalizedLanguage))
        {
            throw new InvalidOperationException(
                $"Translation '{normalizedLanguage}' already exists.");
        }

        _translations.Add(
            new ProductVariantOptionTranslation(Id, normalizedLanguage, name));
    }

    public void SetTranslation(string languageCode, string name)
    {
        var language = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is null)
        {
            AddTranslation(language, name);
            return;
        }

        translation.Update(name);
    }

    public void RemoveTranslation(string languageCode)
    {
        var language = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == language);
        if (translation is not null && _translations.Count > 1)
        {
            _translations.Remove(translation);
        }
    }
}
