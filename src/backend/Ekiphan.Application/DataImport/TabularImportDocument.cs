namespace Ekiphan.Application.DataImport;

public sealed record TabularImportDocument(
    IReadOnlyList<TabularImportSheet> Sheets);

public sealed record TabularImportSheet(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<TabularImportRow> Rows);

public sealed record TabularImportRow(
    int RowNumber,
    IReadOnlyDictionary<string, string?> Values);
