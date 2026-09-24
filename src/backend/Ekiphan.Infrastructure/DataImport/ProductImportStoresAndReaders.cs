using System.Collections.Concurrent;
using Ekiphan.Application.DataImport;

namespace Ekiphan.Infrastructure.DataImport;

internal sealed class ProductImportUploadStore(TimeProvider timeProvider) : IProductImportUploadStore
{
    private readonly ConcurrentDictionary<string, ProductImportStoredUpload> _uploads = new();
    private readonly ConcurrentDictionary<string, ProductImportStoredValidation> _validations = new();

    public string Put(ProductImportStoredUpload upload)
    {
        Cleanup();
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        _uploads[token] = upload;
        return token;
    }

    public bool TryGet(string token, out ProductImportStoredUpload upload)
    {
        Cleanup();
        return _uploads.TryGetValue(token, out upload!);
    }

    public string PutValidation(ProductImportStoredValidation validation)
    {
        Cleanup();
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray());
        _validations[token] = validation;
        return token;
    }

    public bool TryGetValidation(string token, out ProductImportStoredValidation validation)
    {
        Cleanup();
        return _validations.TryGetValue(token, out validation!);
    }

    public void Remove(string token) => _uploads.TryRemove(token, out _);

    private void Cleanup()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var item in _uploads.Where(x => x.Value.ExpiresAt <= now)) _uploads.TryRemove(item.Key, out _);
        foreach (var item in _validations.Where(x => x.Value.ExpiresAt <= now)) _validations.TryRemove(item.Key, out _);
    }
}

internal sealed class CsvProductImportFileReader(ITabularImportFileReader reader) : IProductImportFileReader
{
    public bool CanRead(string fileName) => Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);
    public Task<TabularImportDocument> ReadAsync(Stream content, string fileName, ImportFileReadOptions options,
        CancellationToken cancellationToken = default) => reader.ReadAsync(content, fileName, options, cancellationToken);
}

internal sealed class XlsxProductImportFileReader(ITabularImportFileReader reader) : IProductImportFileReader
{
    public bool CanRead(string fileName) => Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
    public Task<TabularImportDocument> ReadAsync(Stream content, string fileName, ImportFileReadOptions options,
        CancellationToken cancellationToken = default) => reader.ReadAsync(content, fileName, options, cancellationToken);
}
