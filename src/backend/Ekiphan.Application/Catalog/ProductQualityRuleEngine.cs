using Ekiphan.Domain.Catalog;

namespace Ekiphan.Application.Catalog;

public sealed class MissingSkuQualityRule : IProductQualityRule
{
    public string Code => "MISSING_SKU";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        if (string.IsNullOrWhiteSpace(context.Product.SKU))
        {
            issues.Add(new ProductQualityIssueDto(
                Code,
                ProductQualitySeverity.Critical,
                "SKU",
                "Product SKU is missing or empty.",
                "Specify a unique SKU for the product.",
                BlocksPublishing: true));
        }
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class MissingProductNameQualityRule : IProductQualityRule
{
    public string Code => "MISSING_TR_NAME";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        var tr = context.Product.Translations.FirstOrDefault(x => x.LanguageCode == "tr");
        if (tr is null || string.IsNullOrWhiteSpace(tr.Name))
        {
            issues.Add(new ProductQualityIssueDto(
                Code,
                ProductQualitySeverity.Critical,
                "Translations[tr].Name",
                "Turkish product name is missing.",
                "Enter a product name in Turkish.",
                BlocksPublishing: true));
        }
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class MissingPrimaryImageQualityRule : IProductQualityRule
{
    public string Code => "MISSING_PRIMARY_IMAGE";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        // Check variants or media relations if available
        if (context.Product.Variants.Count > 0 && context.Product.Variants.All(v => !v.MediaAssetId.HasValue))
        {
            issues.Add(new ProductQualityIssueDto(
                Code,
                ProductQualitySeverity.Warning,
                "Media",
                "Product variants do not have associated primary images.",
                "Upload and select a primary image for the product.",
                BlocksPublishing: false));
        }
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class MissingCategoryQualityRule : IProductQualityRule
{
    public string Code => "MISSING_CATEGORY";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        if (context.Product.Categories.Count == 0 || !context.Product.PrimaryCategoryId.HasValue)
        {
            issues.Add(new ProductQualityIssueDto(
                Code,
                ProductQualitySeverity.Error,
                "Categories",
                "Product does not have a primary category assigned.",
                "Assign at least one category and set a primary category.",
                BlocksPublishing: true));
        }
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class MissingBrandQualityRule : IProductQualityRule
{
    public string Code => "MISSING_BRAND";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        if (!context.Product.BrandId.HasValue)
        {
            issues.Add(new ProductQualityIssueDto(
                Code,
                ProductQualitySeverity.Information,
                "BrandId",
                "Product brand is not assigned.",
                "Select a brand for the product.",
                BlocksPublishing: false));
        }
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class MissingEnglishTranslationQualityRule : IProductQualityRule
{
    public string Code => "MISSING_EN_TRANSLATION";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        var en = context.Product.Translations.FirstOrDefault(x => x.LanguageCode == "en");
        if (en is null || string.IsNullOrWhiteSpace(en.Name))
        {
            issues.Add(new ProductQualityIssueDto(
                Code,
                ProductQualitySeverity.Warning,
                "Translations[en]",
                "English product translation is missing.",
                "Add English product name and description.",
                BlocksPublishing: false));
        }
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class MissingSeoTitleQualityRule : IProductQualityRule
{
    public string Code => "MISSING_SEO_TITLE";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        var tr = context.Product.Translations.FirstOrDefault(x => x.LanguageCode == "tr");
        if (tr is not null && string.IsNullOrWhiteSpace(tr.MetaTitle))
        {
            issues.Add(new ProductQualityIssueDto(
                Code,
                ProductQualitySeverity.Information,
                "Translations[tr].MetaTitle",
                "Meta title is empty for Turkish translation.",
                "Add a custom meta title for search engines.",
                BlocksPublishing: false));
        }
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class DuplicateSkuQualityRule : IProductQualityRule
{
    public string Code => "DUPLICATE_SKU";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        if (!string.IsNullOrWhiteSpace(context.Product.SKU) &&
            context.AllActiveProducts.Any(p => p.Id != context.Product.Id && string.Equals(p.SKU, context.Product.SKU, StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(new ProductQualityIssueDto(
                Code,
                ProductQualitySeverity.Critical,
                "SKU",
                $"SKU '{context.Product.SKU}' is already used by another active product.",
                "Change the SKU to be unique.",
                BlocksPublishing: true));
        }
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class DuplicateSlugQualityRule : IProductQualityRule
{
    public string Code => "DUPLICATE_SLUG";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        foreach (var translation in context.Product.Translations)
        {
            if (string.IsNullOrWhiteSpace(translation.Slug)) continue;
            var isDuplicate = context.AllActiveProducts.Any(p => p.Id != context.Product.Id &&
                p.Translations.Any(t => t.LanguageCode == translation.LanguageCode &&
                    string.Equals(t.Slug, translation.Slug, StringComparison.OrdinalIgnoreCase)));
            if (isDuplicate)
            {
                issues.Add(new ProductQualityIssueDto(
                    Code,
                    ProductQualitySeverity.Error,
                    $"Translations[{translation.LanguageCode}].Slug",
                    $"Slug '{translation.Slug}' ({translation.LanguageCode}) is already used by another product.",
                    "Update the slug to be unique.",
                    BlocksPublishing: true));
            }
        }
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class MissingRequiredAttributeQualityRule : IProductQualityRule
{
    public string Code => "MISSING_REQUIRED_ATTRIBUTE";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        // Evaluation placeholder for required attribute checks
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}

public sealed class InvalidMediaQualityRule : IProductQualityRule
{
    public string Code => "INVALID_MEDIA";

    public Task<IReadOnlyCollection<ProductQualityIssueDto>> EvaluateAsync(
        ProductQualityContext context,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<ProductQualityIssueDto>();
        return Task.FromResult<IReadOnlyCollection<ProductQualityIssueDto>>(issues);
    }
}
