namespace Ekiphan.Application.DataImport;

public interface IImportReferenceResolver
{
    Task<ImportReferenceResolution> ResolveAsync(
        IReadOnlyCollection<string> brandNames,
        IReadOnlyCollection<IReadOnlyList<string>> categoryPaths,
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
    IReadOnlySet<string> AmbiguousTagNames)
{
    public IReadOnlyDictionary<string, IReadOnlyList<Guid>> CategoryPathIds { get; init; } =
        new Dictionary<string, IReadOnlyList<Guid>>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AmbiguousCategoryPaths { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public static class ImportCategoryPath
{
    public static string Key(IEnumerable<string> names) => string.Join("\u001F", names.Select(value => value.Trim()));
}
