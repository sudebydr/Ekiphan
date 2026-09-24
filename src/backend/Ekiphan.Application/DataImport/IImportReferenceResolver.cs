namespace Ekiphan.Application.DataImport;

public interface IImportReferenceResolver
{
    Task<ImportReferenceResolution> ResolveAsync(
        IReadOnlyCollection<string> brandNames,
        IReadOnlyCollection<string> categoryNames,
        IReadOnlyCollection<string> materialNames,
        IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default);
}

public sealed record ImportReferenceResolution(
    IReadOnlyDictionary<string, Guid> BrandIds,
    IReadOnlyDictionary<string, Guid> CategoryIds,
    IReadOnlySet<string> AmbiguousCategoryNames,
    Guid? MaterialAttributeId,
    IReadOnlyDictionary<string, Guid> MaterialOptionIds,
    IReadOnlySet<string> AmbiguousMaterialNames,
    IReadOnlyDictionary<string, Guid> TagIds,
    IReadOnlySet<string> AmbiguousTagNames);
