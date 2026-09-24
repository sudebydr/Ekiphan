namespace Ekiphan.Application.Catalog;

public sealed record AdminVariantTranslationInput(
    string LanguageCode,
    string Name);

public sealed record SaveVariantGroupCommand(
    string Code,
    int SortOrder,
    IReadOnlyList<AdminVariantTranslationInput> Translations);

public sealed record SaveVariantOptionCommand(
    string Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<AdminVariantTranslationInput> Translations);

public sealed record SaveProductVariantCommand(
    string SKU,
    int SortOrder,
    bool IsActive,
    IReadOnlyDictionary<Guid, Guid> SelectedOptions,
    Guid? MediaAssetId = null);

public sealed record AdminVariantOptionDetail(
    Guid Id,
    string Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<AdminVariantTranslationInput> Translations);

public sealed record AdminVariantGroupDetail(
    Guid Id,
    string Code,
    int SortOrder,
    IReadOnlyList<AdminVariantTranslationInput> Translations,
    IReadOnlyList<AdminVariantOptionDetail> Options);

public sealed record AdminProductVariantDetail(
    Guid Id,
    string SKU,
    int SortOrder,
    bool IsActive,
    IReadOnlyDictionary<Guid, Guid> SelectedOptions,
    Guid? MediaAssetId = null);

public sealed record AdminProductVariantCatalog(
    Guid ProductId,
    string ProductSKU,
    IReadOnlyList<AdminVariantGroupDetail> Groups,
    IReadOnlyList<AdminProductVariantDetail> Variants);

public interface IAdminVariantService
{
    Task<AdminProductVariantCatalog?> GetAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<AdminVariantGroupDetail> CreateGroupAsync(Guid productId, SaveVariantGroupCommand command, CancellationToken cancellationToken = default);
    Task<AdminVariantGroupDetail?> UpdateGroupAsync(Guid productId, Guid groupId, SaveVariantGroupCommand command, CancellationToken cancellationToken = default);
    Task<AdminVariantOptionDetail> CreateOptionAsync(Guid productId, Guid groupId, SaveVariantOptionCommand command, CancellationToken cancellationToken = default);
    Task<AdminVariantOptionDetail?> UpdateOptionAsync(Guid productId, Guid optionId, SaveVariantOptionCommand command, CancellationToken cancellationToken = default);
    Task<AdminProductVariantDetail> CreateVariantAsync(Guid productId, SaveProductVariantCommand command, CancellationToken cancellationToken = default);
    Task<AdminProductVariantDetail?> UpdateVariantAsync(Guid productId, Guid variantId, SaveProductVariantCommand command, CancellationToken cancellationToken = default);
}

public sealed class AdminVariantConflictException(string message)
    : Exception(message);
