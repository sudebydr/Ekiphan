namespace Ekiphan.Application.Catalog;

public sealed record AdminProductTranslationInput(
    string LanguageCode,
    string Name,
    string Slug,
    string? ShortDescription,
    string? LongDescription,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? CanonicalUrl = null,
    bool NoIndex = false,
    bool NoFollow = false,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    Guid? OpenGraphImageMediaId = null);

public sealed record AdminProductAttributeValueInput(
    Guid AttributeId,
    int Sequence,
    string? TextValue,
    decimal? NumericValue,
    bool? BooleanValue,
    Guid? AttributeOptionId,
    Guid? UnitId);

public sealed record SaveAdminProductCommand(
    string SKU,
    Guid? BrandId,
    bool IsPublished,
    IReadOnlyList<AdminProductTranslationInput> Translations,
    IReadOnlyList<Guid>? CategoryIds = null,
    Guid? PrimaryCategoryId = null,
    IReadOnlyList<AdminProductAttributeValueInput>? AttributeValues = null,
    IReadOnlyList<Guid>? TagIds = null);

public sealed record AdminProductTranslation(
    string LanguageCode,
    string Name,
    string Slug,
    string? ShortDescription,
    string? LongDescription,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? CanonicalUrl = null,
    bool NoIndex = false,
    bool NoFollow = false,
    string? OpenGraphTitle = null,
    string? OpenGraphDescription = null,
    Guid? OpenGraphImageMediaId = null,
    string? OpenGraphImageUrl = null);

public sealed record AdminProductDetail(
    Guid Id,
    string SKU,
    Guid? BrandId,
    bool IsPublished,
    IReadOnlyList<AdminProductTranslation> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<Guid>? CategoryIds = null,
    Guid? PrimaryCategoryId = null,
    IReadOnlyList<AdminProductAttributeValueInput>? AttributeValues = null,
    IReadOnlyList<Guid>? TagIds = null);

public interface IAdminProductManagementService
{
    Task<AdminProductDetail?> GetAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetail> CreateAsync(
        SaveAdminProductCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetail?> UpdateAsync(
        Guid productId,
        SaveAdminProductCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}

public sealed class AdminProductConflictException(string message)
    : Exception(message);
