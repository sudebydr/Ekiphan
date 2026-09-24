namespace Ekiphan.Application.DataImport;

public sealed record ImportFileReadOptions
{
    public const long DefaultMaximumFileSizeBytes = 25 * 1024 * 1024;

    public long MaximumFileSizeBytes { get; init; } = DefaultMaximumFileSizeBytes;

    public int MaximumRows { get; init; } = 50_000;

    public int MaximumColumns { get; init; } = 250;

    public int MaximumCellLength { get; init; } = 32_767;
}
