using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class Category : Entity
{
    private readonly List<CategoryTranslation> _translations = [];

    private Category()
    {
    }

    public Category(
        Guid id,
        Guid productSectionId,
        Guid? parentId = null,
        int sortOrder = 0)
        : base(id)
    {
        if (productSectionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product section identifier cannot be empty.",
                nameof(productSectionId));
        }

        if (parentId == id)
        {
            throw new ArgumentException("A category cannot be its own parent.", nameof(parentId));
        }

        ProductSectionId = productSectionId;
        ParentId = parentId;
        SortOrder = sortOrder;
    }

    public Guid ProductSectionId { get; private set; }

    public Guid? ParentId { get; private set; }

    public bool IsPublished { get; private set; }

    public int SortOrder { get; private set; }

    public IReadOnlyCollection<CategoryTranslation> Translations => _translations;

    public void SetPublished(bool isPublished) => IsPublished = isPublished;

    public void Update(Guid productSectionId, Guid? parentId, int sortOrder)
    {
        if (productSectionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product section identifier cannot be empty.",
                nameof(productSectionId));
        }

        if (parentId == Id)
        {
            throw new ArgumentException(
                "A category cannot be its own parent.",
                nameof(parentId));
        }

        ProductSectionId = productSectionId;
        ParentId = parentId;
        SortOrder = sortOrder;
    }

    public void AddTranslation(
        string languageCode,
        string name,
        string slug,
        string? description = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool noIndex = false,
        bool noFollow = false,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);

        if (_translations.Any(item => item.LanguageCode == normalizedLanguage))
        {
            throw new InvalidOperationException(
                $"Translation '{normalizedLanguage}' already exists.");
        }

        _translations.Add(
            new CategoryTranslation(
                Id, normalizedLanguage, name, slug, description,
                metaTitle, metaDescription, canonicalUrl, noIndex, noFollow,
                openGraphTitle, openGraphDescription, openGraphImageMediaId));
    }

    public void SetTranslation(
        string languageCode,
        string name,
        string slug,
        string? description = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool noIndex = false,
        bool noFollow = false,
        string? openGraphTitle = null,
        string? openGraphDescription = null,
        Guid? openGraphImageMediaId = null)
    {
        var normalizedLanguage = CatalogGuard.LanguageCode(languageCode);
        var translation = _translations.SingleOrDefault(
            item => item.LanguageCode == normalizedLanguage);
        if (translation is null)
        {
            AddTranslation(
                normalizedLanguage, name, slug, description,
                metaTitle, metaDescription, canonicalUrl, noIndex, noFollow,
                openGraphTitle, openGraphDescription, openGraphImageMediaId);
            return;
        }

        translation.Update(
            name, slug, description,
            metaTitle, metaDescription, canonicalUrl, noIndex, noFollow,
            openGraphTitle, openGraphDescription, openGraphImageMediaId);
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
                "A category must keep at least one translation.");
        }

        _translations.Remove(translation);
    }
}
