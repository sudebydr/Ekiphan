namespace Ekiphan.Application.DataImport;

public interface ITabularImportFileReader
{
    Task<TabularImportDocument> ReadAsync(
        Stream content,
        string fileName,
        ImportFileReadOptions? options = null,
        CancellationToken cancellationToken = default);
}
