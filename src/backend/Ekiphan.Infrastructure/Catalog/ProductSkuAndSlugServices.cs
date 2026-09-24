using System.Text;
using Ekiphan.Application.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

public sealed class ProductSkuNormalizationService : IProductSkuNormalizationService
{
    public string Normalize(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return string.Empty;

        // Trim leading/trailing whitespace
        var trimmed = sku.Trim();

        // Remove invisible control characters
        var cleaned = new string(trimmed.Where(c => !char.IsControl(c)).ToArray());

        // Normalize Unicode string (Form C) and convert to uppercase invariant
        return cleaned.Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}

public sealed class ProductSlugValidationService(EkiphanDbContext dbContext) : IProductSlugValidationService
{
    public async Task<bool> IsSlugUniqueAsync(
        string slug,
        string languageCode,
        Guid? excludeProductId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var normalizedLang = languageCode.Trim().ToLowerInvariant();

        return !await dbContext.Products.AsNoTracking()
            .Where(p => !p.IsDeleted && (!excludeProductId.HasValue || p.Id != excludeProductId.Value))
            .SelectMany(p => p.Translations)
            .AnyAsync(t => t.LanguageCode == normalizedLang && t.Slug == normalizedSlug, cancellationToken);
    }
}
