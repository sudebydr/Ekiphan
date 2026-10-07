using Ekiphan.Application.Media;
using Ekiphan.Application.MediaImport;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.MediaImport;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ekiphan.UnitTests.Media;

public sealed class ProductMediaImportExecutionServiceTests
{
    [Fact]
    public async Task ExecuteImportsValidMatchedFileAndKeepsSkipAndFailureCountsExclusive()
    {
        var productId = Guid.NewGuid();
        var batch = new ProductMediaImportBatch(Guid.NewGuid(), "images.zip", Hash, Guid.NewGuid());
        var validItem = Item(batch.Id, "valid", productId);
        var skippedItem = new ProductMediaImportBatchItem(Guid.NewGuid(), batch.Id, "skipped", "missing.jpg", null, Hash, 1, false);
        skippedItem.SetFailure(ProductMediaImportBatchItemStatus.Skipped, "PRODUCT_NOT_FOUND", "Missing.");
        batch.AddItem(validItem); batch.AddItem(skippedItem);
        var temporary = new TemporaryStorage();
        temporary.Files["valid"] = Jpeg;
        var archive = new ProductMediaArchiveReadResult("container", "images.zip", Jpeg.Length, Hash, 2,
        [
            new("valid", "valid.jpg", "SKU.jpg", ".jpg", "image/jpeg", Jpeg.Length, Jpeg.Length, Hash,
                new("SKU", ProductMediaDetectedPosition.Gallery, 1, true), ProductMediaPreviewFileStatus.Ready, null, null),
            new("skipped", "missing.jpg", "MISSING.jpg", ".jpg", "image/jpeg", Jpeg.Length, Jpeg.Length, Hash,
                new("MISSING", ProductMediaDetectedPosition.Gallery, 1, false), ProductMediaPreviewFileStatus.Ready, null, null)
        ]);
        var clock = TimeProvider.System;
        var tokens = new ProductMediaImportTokenService(clock);
        var uploadToken = tokens.CreateUpload(new(batch.Id, archive, Guid.NewGuid(), clock.GetUtcNow().AddMinutes(10)));
        var validFile = new ProductMediaImportFileValidationDto("valid", "valid.jpg", "SKU", productId, "SKU", "SKU",
            ProductMediaMatchSource.FileName, 1, true, Hash, ProductMediaValidationFileStatus.Valid, [], []);
        var skippedFile = validFile with { TemporaryFileId = "skipped", Status = ProductMediaValidationFileStatus.Unmatched, MatchedProductId = null };
        var validationToken = tokens.CreateValidation(new(batch.Id, uploadToken, new(), [validFile, skippedFile], clock.GetUtcNow().AddMinutes(10)));
        var repository = new Repository(batch);
        var mediaStorage = new MediaStorage();
        var mediaUpload = new MediaUploadService(new MediaFileSignatureValidator(), new Scanner(), mediaStorage, new AssetRepository(), clock);
        var service = new ProductMediaImportExecutionService(tokens, repository, temporary, mediaUpload, mediaStorage,
            new ProductMediaImportExecuteCommandValidator(), clock, NullLogger<ProductMediaImportExecutionService>.Instance);

        var result = await service.ExecuteAsync(new(validationToken));

        Assert.Equal(1, result.ImportedFiles);
        Assert.Equal(1, result.SkippedFiles);
        Assert.Equal(0, result.ErrorFiles);
        Assert.Single(repository.Links);
        Assert.NotEqual(Guid.Empty, repository.Links[0].MediaAssetId);
    }

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly string Hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Jpeg));
    private static ProductMediaImportBatchItem Item(Guid batchId, string temporaryId, Guid productId)
    {
        var item = new ProductMediaImportBatchItem(Guid.NewGuid(), batchId, temporaryId, "valid.jpg", "SKU", Hash, 1, true);
        item.Match(productId, 1, true); return item;
    }

    private sealed class Scanner : IMediaThreatScanner { public Task<MediaThreatScanStatus> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default) => Task.FromResult(MediaThreatScanStatus.Clean); }
    private sealed class AssetRepository : IMediaAssetRepository { public Task AddAsync(MediaAsset asset, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class MediaStorage : IMediaFileStorage { public bool IsConfigured => true; public Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class TemporaryStorage : ITemporaryProductMediaStorage
    {
        public Dictionary<string, byte[]> Files { get; } = new();
        public Task StoreFileAsync(string containerId, string temporaryFileId, Stream content, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Stream> OpenReadAsync(string containerId, string temporaryFileId, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream(Files[temporaryFileId]));
        public Task DeleteAsync(string containerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class Repository(ProductMediaImportBatch batch) : IProductMediaImportRepository
    {
        public List<(Guid ProductId, Guid MediaAssetId)> Links { get; } = [];
        public Task AddBatchAsync(ProductMediaImportBatch value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ProductMediaProductMatch>> FindProductsAsync(IReadOnlyCollection<string> skus, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProductMediaProductMatch>>([]);
        public Task<IReadOnlyDictionary<string, Guid>> FindMediaAssetsByHashesAsync(IReadOnlyCollection<string> hashes, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<string, Guid>>(new Dictionary<string, Guid>());
        public Task<IReadOnlySet<ProductMediaContentLink>> FindProductMediaContentLinksAsync(IReadOnlyCollection<Guid> productIds, IReadOnlyCollection<string> hashes, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<ProductMediaContentLink>>(new HashSet<ProductMediaContentLink>());
        public Task<ProductMediaImportBatch?> GetBatchAsync(Guid id, bool tracked, CancellationToken cancellationToken = default) => Task.FromResult<ProductMediaImportBatch?>(batch);
        public Task<string?> GetProductNameAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) => action(cancellationToken);
        public Task AddProductMediaAsync(Guid productId, Guid mediaAssetId, Guid batchId, int sortOrder, bool isPrimary, bool replaceExistingPrimary, DateTimeOffset now, CancellationToken cancellationToken = default) { Links.Add((productId, mediaAssetId)); return Task.CompletedTask; }
        public Task<(bool Removed, bool ArchiveAsset, string? StorageKey)> RollbackItemAsync(ProductMediaImportBatchItem item, DateTimeOffset now, CancellationToken cancellationToken = default) => Task.FromResult((false, false, (string?)null));
        public Task<string?> ArchiveUnassignedMediaAsync(Guid mediaAssetId, DateTimeOffset now, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
}
