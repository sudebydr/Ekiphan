using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class ProductVariantGroup : Entity
{
    private readonly List<ProductVariantGroupTranslation> _translations = [];
    private readonly List<ProductVariantOption> _options = [];

    private ProductVariantGroup()
    {
    }

    internal ProductVariantGroup(
        Guid id,
        Guid productId,
        string code,
        int sortOrder)
        : base(id)
    {
        ProductId = productId;
        Code = CatalogGuard.Required(code, 100, nameof(code)).ToUpperInvariant();
        SortOrder = sortOrder;
    }

    public Guid ProductId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<ProductVariantGroupTranslation> Translations =>
        _translations;

    public IReadOnlyCollection<ProductVariantOption> Options => _options;

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
            new ProductVariantGroupTranslation(Id, normalizedLanguage, name));
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

    public ProductVariantOption AddOption(
        Guid optionId,
        string code,
        int sortOrder = 0)
    {
        var normalizedCode = CatalogGuard.Required(code, 100, nameof(code))
            .ToUpperInvariant();

        if (_options.Any(option => option.Code == normalizedCode))
        {
            throw new InvalidOperationException(
                $"Variant option '{normalizedCode}' already exists.");
        }

        var option = new ProductVariantOption(
            optionId,
            Id,
            normalizedCode,
            sortOrder);
        _options.Add(option);
        return option;
    }
}
