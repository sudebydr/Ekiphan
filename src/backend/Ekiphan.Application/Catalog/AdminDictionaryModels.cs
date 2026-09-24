namespace Ekiphan.Application.Catalog;

public sealed record AdminTagTranslationInput(
    string LanguageCode,
    string Name,
    string Slug);

public sealed record SaveAdminTagCommand(
    string Code,
    bool IsActive,
    IReadOnlyList<AdminTagTranslationInput> Translations);

public sealed record AdminTagDetail(
    Guid Id,
    string Code,
    bool IsActive,
    IReadOnlyList<AdminTagTranslationInput> Translations);

public sealed record SaveAdminUnitCommand(
    string Code,
    string Symbol,
    string Dimension,
    decimal ConversionFactorToBase,
    bool IsBaseUnit,
    bool IsActive);

public sealed record AdminUnitDefinitionDetail(
    Guid Id,
    string Code,
    string Symbol,
    string Dimension,
    decimal ConversionFactorToBase,
    bool IsBaseUnit,
    bool IsActive);

public sealed record AdminProductTagAssignment(
    Guid ProductId,
    IReadOnlyList<Guid> TagIds);

public sealed record AdminDictionaryCatalog(
    IReadOnlyList<AdminTagDetail> Tags,
    IReadOnlyList<AdminUnitDefinitionDetail> Units,
    IReadOnlyList<AdminProductTagAssignment> ProductTags);

public interface IAdminDictionaryService
{
    Task<AdminDictionaryCatalog> GetAsync(CancellationToken cancellationToken = default);
    Task<AdminTagDetail> CreateTagAsync(SaveAdminTagCommand command, CancellationToken cancellationToken = default);
    Task<AdminTagDetail?> UpdateTagAsync(Guid tagId, SaveAdminTagCommand command, CancellationToken cancellationToken = default);
    Task<AdminUnitDefinitionDetail> CreateUnitAsync(SaveAdminUnitCommand command, CancellationToken cancellationToken = default);
    Task<AdminUnitDefinitionDetail?> UpdateUnitAsync(Guid unitId, SaveAdminUnitCommand command, CancellationToken cancellationToken = default);
    Task<AdminProductTagAssignment?> SaveProductTagsAsync(Guid productId, IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken = default);
}

public sealed class AdminDictionaryConflictException(string message)
    : Exception(message);
