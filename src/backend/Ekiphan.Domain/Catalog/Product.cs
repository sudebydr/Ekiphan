using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Catalog;

public sealed class Product : Entity
{
    private readonly List<ProductCategory> _categories = [];
    private readonly List<ProductTranslation> _translations = [];
    private readonly List<ProductTag> _tags = [];
    private readonly List<ProductVariantGroup> _variantGroups = [];
    private readonly List<ProductVariant> _variants = [];

    private Product()
    {
    }

    public Product(Guid id, string sku, Guid? brandId = null)
        : base(id)
    {
        SKU = SkuNormalizer.Normalize(CatalogGuard.Required(sku, 100, nameof(sku)));
        NormalizedSku = SKU;
        BrandId = brandId;
        WorkflowStatus = ProductWorkflowStatus.Draft;
        VersionNumber = 1;
        RowVersion = Array.Empty<byte>();
    }

    public string SKU { get; private set; } = string.Empty;

    public string NormalizedSku { get; private set; } = string.Empty;

    public Guid? BrandId { get; private set; }

    public Guid? PrimaryCategoryId { get; private set; }

    public bool IsPublished { get; private set; }

    public ProductWorkflowStatus WorkflowStatus { get; private set; } = ProductWorkflowStatus.Draft;

    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid? PublishedByUserId { get; private set; }

    public DateTimeOffset? SubmittedForReviewAt { get; private set; }
    public Guid? SubmittedForReviewByUserId { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }
    public Guid? ArchivedByUserId { get; private set; }
    public string? ArchiveReason { get; private set; }

    public DateTimeOffset? LastQualityCheckAt { get; private set; }
    public int? QualityScore { get; private set; }

    public int VersionNumber { get; private set; } = 1;

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public bool IsDeleted { get; private set; }

    public Guid? ImportedByBatchId { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<ProductCategory> Categories => _categories;

    public IReadOnlyCollection<ProductTranslation> Translations => _translations;

    public IReadOnlyCollection<ProductTag> Tags => _tags;

    public IReadOnlyCollection<ProductVariantGroup> VariantGroups => _variantGroups;

    public IReadOnlyCollection<ProductVariant> Variants => _variants;

    public void SetPublished(bool isPublished)
    {
        IsPublished = isPublished;
        WorkflowStatus = isPublished ? ProductWorkflowStatus.Published : ProductWorkflowStatus.Unpublished;
    }

    public void SetWorkflowStatus(
        ProductWorkflowStatus targetStatus,
        Guid? actorUserId,
        string? reason = null,
        DateTimeOffset? timestamp = null)
    {
        var now = timestamp ?? DateTimeOffset.UtcNow;
        WorkflowStatus = targetStatus;
        IsPublished = targetStatus == ProductWorkflowStatus.Published;

        switch (targetStatus)
        {
            case ProductWorkflowStatus.InReview:
                SubmittedForReviewAt = now;
                SubmittedForReviewByUserId = actorUserId;
                break;
            case ProductWorkflowStatus.Published:
                PublishedAt = now;
                PublishedByUserId = actorUserId;
                ReviewedAt = now;
                ReviewedByUserId = actorUserId;
                break;
            case ProductWorkflowStatus.Unpublished:
                // Remains unpublished
                break;
            case ProductWorkflowStatus.Archived:
                ArchivedAt = now;
                ArchivedByUserId = actorUserId;
                ArchiveReason = reason;
                break;
        }
    }

    public void SetQualityScore(int score, DateTimeOffset checkedAt)
    {
        QualityScore = Math.Clamp(score, 0, 100);
        LastQualityCheckAt = checkedAt;
    }

    public void IncrementVersion()
    {
        VersionNumber++;
    }

    public void MarkImported(Guid batchId)
    {
        if (batchId == Guid.Empty) throw new ArgumentException("Import batch is required.", nameof(batchId));
        ImportedByBatchId = batchId;
        IsPublished = false;
    }

    public void UpdateIdentity(string sku, Guid? brandId)
    {
        SKU = SkuNormalizer.Normalize(CatalogGuard.Required(sku, 100, nameof(sku)));
        NormalizedSku = SKU;
        BrandId = brandId;
    }

    public void AddCategory(Guid categoryId, bool isPrimary = false, int sortOrder = 0)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category identifier cannot be empty.",
                nameof(categoryId));
        }

        if (_categories.Any(item => item.CategoryId == categoryId))
        {
            throw new InvalidOperationException("Product is already assigned to this category.");
        }

        if (isPrimary)
        {
            foreach (var category in _categories)
            {
                category.SetPrimary(false);
            }

            PrimaryCategoryId = categoryId;
        }

        _categories.Add(new ProductCategory(Id, categoryId, isPrimary, sortOrder));
    }

    public void SetPrimaryCategory(Guid categoryId)
    {
        var selectedCategory = _categories.SingleOrDefault(
            item => item.CategoryId == categoryId);

        if (selectedCategory is null)
        {
            throw new InvalidOperationException(
                "Primary category must already be assigned to the product.");
        }

        foreach (var category in _categories)
        {
            category.SetPrimary(category.CategoryId == categoryId);
        }

        PrimaryCategoryId = categoryId;
    }

    public void SetCategories(
        IReadOnlyCollection<Guid> categoryIds,
        Guid? primaryCategoryId)
    {
        ArgumentNullException.ThrowIfNull(categoryIds);
        if (categoryIds.Any(item => item == Guid.Empty) ||
            categoryIds.Distinct().Count() != categoryIds.Count)
        {
            throw new ArgumentException(
                "Category identifiers must be non-empty and unique.",
                nameof(categoryIds));
        }

        if (primaryCategoryId.HasValue &&
            !categoryIds.Contains(primaryCategoryId.Value))
        {
            throw new ArgumentException(
                "Primary category must be included in category identifiers.",
                nameof(primaryCategoryId));
        }

        _categories.RemoveAll(item => !categoryIds.Contains(item.CategoryId));
        var sortOrder = 0;
        foreach (var categoryId in categoryIds)
        {
            var category = _categories.SingleOrDefault(
                item => item.CategoryId == categoryId);
            if (category is null)
            {
                category = new ProductCategory(
                    Id,
                    categoryId,
                    false,
                    sortOrder);
                _categories.Add(category);
            }

            category.SetPrimary(categoryId == primaryCategoryId);
            category.SetSortOrder(sortOrder++);
        }

        PrimaryCategoryId = primaryCategoryId;
    }

    public void AddTranslation(
        string languageCode,
        string name,
        string slug,
        string? shortDescription = null,
        string? longDescription = null,
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
            new ProductTranslation(
                Id,
                normalizedLanguage,
                name,
                slug,
                shortDescription,
                longDescription,
                metaTitle,
                metaDescription,
                canonicalUrl,
                noIndex,
                noFollow,
                openGraphTitle,
                openGraphDescription,
                openGraphImageMediaId));
    }

    public void SetTranslation(
        string languageCode,
        string name,
        string slug,
        string? shortDescription = null,
        string? longDescription = null,
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
                normalizedLanguage,
                name,
                slug,
                shortDescription,
                longDescription,
                metaTitle,
                metaDescription,
                canonicalUrl,
                noIndex,
                noFollow,
                openGraphTitle,
                openGraphDescription,
                openGraphImageMediaId);
            return;
        }

        translation.Update(
            name,
            slug,
            shortDescription,
            longDescription,
            metaTitle,
            metaDescription,
            canonicalUrl,
            noIndex,
            noFollow,
            openGraphTitle,
            openGraphDescription,
            openGraphImageMediaId);
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
                "A product must keep at least one translation.");
        }

        _translations.Remove(translation);
    }

    public void AddTag(Guid tagId, int sortOrder = 0)
    {
        if (_tags.Any(item => item.TagId == tagId))
        {
            throw new InvalidOperationException(
                "Product is already assigned to this tag.");
        }

        _tags.Add(new ProductTag(Id, tagId, sortOrder));
    }

    public void SetTags(IReadOnlyCollection<Guid> tagIds)
    {
        ArgumentNullException.ThrowIfNull(tagIds);
        if (tagIds.Any(item => item == Guid.Empty) ||
            tagIds.Distinct().Count() != tagIds.Count)
        {
            throw new ArgumentException(
                "Tag identifiers must be non-empty and unique.",
                nameof(tagIds));
        }

        _tags.RemoveAll(item => !tagIds.Contains(item.TagId));
        var sortOrder = 0;
        foreach (var tagId in tagIds)
        {
            var tag = _tags.SingleOrDefault(item => item.TagId == tagId);
            if (tag is null)
            {
                tag = new ProductTag(Id, tagId, sortOrder);
                _tags.Add(tag);
            }

            tag.SetSortOrder(sortOrder++);
        }
    }

    public void SoftDelete(DateTimeOffset deletedAt)
    {
        IsDeleted = true;
        DeletedAt = deletedAt;
        IsPublished = false;
    }

    public ProductVariantGroup AddVariantGroup(
        Guid groupId,
        string code,
        int sortOrder = 0)
    {
        const int maximumVariantGroups = 2;

        if (_variants.Count > 0)
        {
            throw new InvalidOperationException(
                "Variant groups must be defined before variant SKUs are created.");
        }

        if (_variantGroups.Count >= maximumVariantGroups)
        {
            throw new InvalidOperationException(
                "A product can contain at most two variant groups.");
        }

        var normalizedCode = CatalogGuard.Required(code, 100, nameof(code))
            .ToUpperInvariant();

        if (_variantGroups.Any(group => group.Code == normalizedCode))
        {
            throw new InvalidOperationException(
                $"Variant group '{normalizedCode}' already exists.");
        }

        var group = new ProductVariantGroup(groupId, Id, normalizedCode, sortOrder);
        _variantGroups.Add(group);
        return group;
    }

    public ProductVariant AddVariant(
        Guid variantId,
        string sku,
        IReadOnlyDictionary<Guid, Guid> selectedOptions,
        int sortOrder = 0,
        Guid? mediaAssetId = null)
    {
        ArgumentNullException.ThrowIfNull(selectedOptions);

        if (_variantGroups.Count == 0)
        {
            throw new InvalidOperationException(
                "A variant requires at least one variant group.");
        }

        if (selectedOptions.Count != _variantGroups.Count ||
            _variantGroups.Any(group => !selectedOptions.ContainsKey(group.Id)))
        {
            throw new InvalidOperationException(
                "A variant must select exactly one option from every variant group.");
        }

        foreach (var group in _variantGroups)
        {
            if (!group.Options.Any(option =>
                    option.Id == selectedOptions[group.Id] &&
                    option.IsActive))
            {
                throw new InvalidOperationException(
                    $"Selected option does not belong to variant group '{group.Code}'.");
            }
        }

        var normalizedSku = CatalogGuard.Required(sku, 100, nameof(sku))
            .ToUpperInvariant();

        if (_variants.Any(variant => variant.SKU == normalizedSku))
        {
            throw new InvalidOperationException(
                $"Variant SKU '{normalizedSku}' already exists on this product.");
        }

        if (_variants.Any(variant => variant.HasSameSelection(selectedOptions)))
        {
            throw new InvalidOperationException(
                "The selected variant combination already exists.");
        }

        var variant = new ProductVariant(
            variantId,
            Id,
            normalizedSku,
            selectedOptions,
            sortOrder,
            mediaAssetId);
        _variants.Add(variant);
        return variant;
    }

    public void UpdateVariant(
        Guid variantId,
        string sku,
        IReadOnlyDictionary<Guid, Guid> selectedOptions,
        int sortOrder,
        bool isActive,
        Guid? mediaAssetId = null)
    {
        ArgumentNullException.ThrowIfNull(selectedOptions);
        var variant = _variants.SingleOrDefault(item => item.Id == variantId)
            ?? throw new InvalidOperationException("Variant does not exist.");
        if (selectedOptions.Count != _variantGroups.Count ||
            _variantGroups.Any(group => !selectedOptions.ContainsKey(group.Id)))
        {
            throw new InvalidOperationException(
                "A variant must select exactly one option from every variant group.");
        }

        foreach (var group in _variantGroups)
        {
            if (!group.Options.Any(option =>
                    option.Id == selectedOptions[group.Id] &&
                    (option.IsActive ||
                     variant.Selections.Any(selection =>
                         selection.VariantGroupId == group.Id &&
                         selection.VariantOptionId == option.Id))))
            {
                throw new InvalidOperationException(
                    $"Selected option does not belong to variant group '{group.Code}'.");
            }
        }

        var normalizedSku = CatalogGuard.Required(sku, 100, nameof(sku))
            .ToUpperInvariant();
        if (_variants.Any(item =>
                item.Id != variantId &&
                (item.SKU == normalizedSku ||
                 item.HasSameSelection(selectedOptions))))
        {
            throw new InvalidOperationException(
                "Variant SKU and option combination must be unique.");
        }

        variant.Update(
            normalizedSku,
            selectedOptions,
            sortOrder,
            mediaAssetId);
        variant.SetActive(isActive);
    }
}
