namespace Ekiphan.Application.Catalog;

public sealed record AdminAttributeTranslationInput(
    string LanguageCode,
    string Name);

public sealed record SaveAdminAttributeCommand(
    string Code,
    string DataType,
    string? UnitDimension,
    bool IsActive,
    IReadOnlyList<AdminAttributeTranslationInput> Translations);

public sealed record AdminAttributeOptionInput(
    string Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<AdminAttributeTranslationInput> Translations);

public sealed record AdminAttributeOptionDetail(
    Guid Id,
    string Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<AdminAttributeTranslationInput> Translations);

public sealed record AdminAttributeDetail(
    Guid Id,
    string Code,
    string DataType,
    string? UnitDimension,
    bool IsActive,
    IReadOnlyList<AdminAttributeTranslationInput> Translations,
    IReadOnlyList<AdminAttributeOptionDetail> Options);

public sealed record SaveCategoryAttributeCommand(
    Guid CategoryId,
    Guid AttributeId,
    bool IsRequired,
    bool IsFilterable,
    bool IsVisibleOnProduct,
    bool IsVisibleOnComparison,
    int SortOrder);

public sealed record AdminCategoryAttributeDetail(
    Guid CategoryId,
    Guid AttributeId,
    bool IsRequired,
    bool IsFilterable,
    bool IsVisibleOnProduct,
    bool IsVisibleOnComparison,
    int SortOrder);

public sealed record AdminAttributeCatalog(
    IReadOnlyList<AdminAttributeDetail> Attributes,
    IReadOnlyList<AdminCategoryAttributeDetail> Assignments,
    IReadOnlyList<AdminUnitDetail> Units);

public sealed record AdminUnitDetail(
    Guid Id,
    string Code,
    string Symbol,
    string Dimension,
    bool IsActive);

public interface IAdminAttributeService
{
    Task<AdminAttributeCatalog> GetAsync(CancellationToken cancellationToken = default);
    Task<AdminAttributeDetail> CreateAsync(SaveAdminAttributeCommand command, CancellationToken cancellationToken = default);
    Task<AdminAttributeDetail?> UpdateAsync(Guid id, SaveAdminAttributeCommand command, CancellationToken cancellationToken = default);
    Task<AdminAttributeOptionDetail> CreateOptionAsync(Guid attributeId, AdminAttributeOptionInput command, CancellationToken cancellationToken = default);
    Task<AdminAttributeOptionDetail?> UpdateOptionAsync(Guid optionId, AdminAttributeOptionInput command, CancellationToken cancellationToken = default);
    Task<AdminCategoryAttributeDetail> SaveAssignmentAsync(SaveCategoryAttributeCommand command, CancellationToken cancellationToken = default);
    Task<bool> RemoveAssignmentAsync(Guid categoryId, Guid attributeId, CancellationToken cancellationToken = default);
}

public sealed class AdminAttributeConflictException(string message)
    : Exception(message);
