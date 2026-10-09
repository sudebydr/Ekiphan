using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using Ekiphan.Api.Media;
using Ekiphan.Application.MediaImport;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.MediaImport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ekiphan.UnitTests.Media;

public sealed class ZipProductMediaSessionTests
{
    [Fact]
    public async Task ExpiredTokensAndLostExtractedFilesRenewAfterRestartWithoutCreatingBatchOrReuploading()
    {
        var root = Path.Combine(Path.GetTempPath(), "zip-renew-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var store = new ZipUploadStore(new() { Root = root });
            var owner = Guid.NewGuid();
            byte[] image = [137, 80, 78, 71, 13, 10, 26, 10];
            using var memory = new MemoryStream();
            using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
            { using var entry = zip.CreateEntry("test.png").Open(); entry.Write(image); }
            var bytes = memory.ToArray();
            var state = await store.CreateAsync(owner, "product", "test.zip", bytes.Length, new string('A', 64), bytes.Length, default);
            await File.WriteAllBytesAsync(store.ArchivePath(state.Id), bytes);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            var batch = new ProductMediaImportBatch(Guid.NewGuid(), "test.zip", hash, owner);
            var archive = new ProductMediaArchiveReadResult("expired-files", "test.zip", bytes.Length, hash, 1,
                [new("original-item-id", "test.png", "test.png", ".png", "image/png", image.Length, image.Length,
                    Convert.ToHexString(SHA256.HashData(image)), new("TEST", ProductMediaDetectedPosition.Main, 0, true), ProductMediaPreviewFileStatus.Ready, null, null)]);
            var clock = new Clock();
            var tokens = new ProductMediaImportTokenService(clock);
            var uploadToken = tokens.CreateUpload(new(batch.Id, archive, owner, clock.GetUtcNow().AddMinutes(60)));
            var repository = DispatchProxy.Create<IProductMediaImportRepository, RepositoryProxy>();
            ((RepositoryProxy)(object)repository).Batch = batch;
            var storage = new Storage();
            using var provider = Provider(tokens, repository, storage, batch);
            var session = new ZipProductMediaSession(store, provider);
            await session.CapturePreviewAsync(state, new(uploadToken, "test.zip", bytes.Length, 1, 1, 0, 0, 0, 0, 1, [], "ok"), default);
            clock.Now += TimeSpan.FromHours(3);
            Assert.False(tokens.TryGetUpload(uploadToken, out _));
            clock.Now = DateTimeOffset.UtcNow;
            var validation = await session.ValidateAsync(state, default);
            clock.Now += TimeSpan.FromDays(2);
            Assert.False(tokens.TryGetValidation(validation.ValidationToken, out _));
            // Simulate process restart: new in-memory token store, persisted session receipts only.
            var restartedTokens = new ProductMediaImportTokenService(TimeProvider.System);
            using var restarted = Provider(restartedTokens, repository, storage, batch);
            await using var retainedZip = File.OpenRead(store.ArchivePath(state.Id));
            var result = await new ZipProductMediaSession(store, restarted).ExecuteAsync(state, retainedZip, default);
            Assert.Equal(1, result.ImportedFiles);
            Assert.Equal(1, storage.Stored);
            Assert.Equal("original-item-id", storage.LastId);
            Assert.Equal(0, result.ErrorFiles);
            await Assert.ThrowsAsync<ProductMediaImportConflictException>(() =>
                new ZipProductMediaSession(store, restarted).ValidateAsync(state with { Owner = Guid.NewGuid() }, default));
            batch.Complete(DateTimeOffset.UtcNow, 1, 0, 0);
            await Assert.ThrowsAsync<ProductMediaImportConflictException>(() => session.ValidateAsync(state, default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private static ServiceProvider Provider(IProductMediaImportTokenService tokens, IProductMediaImportRepository repository,
        Storage storage, ProductMediaImportBatch batch) => new ServiceCollection()
        .AddSingleton(tokens).AddSingleton(repository).AddSingleton<ITemporaryProductMediaStorage>(storage)
        .AddSingleton<IOptions<ProductMediaImportOptions>>(Options.Create(new ProductMediaImportOptions()))
        .AddSingleton<IProductMediaImportValidationService>(new Validation(tokens, batch))
        .AddSingleton<IProductMediaImportExecutionService>(new Execution(tokens, storage)).BuildServiceProvider();
    public class RepositoryProxy : DispatchProxy
    {
        public ProductMediaImportBatch Batch { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name == "GetBatchAsync"
            ? Task.FromResult<ProductMediaImportBatch?>(Batch) : throw new InvalidOperationException("Unexpected database mutation/query: " + targetMethod?.Name);
    }
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Storage : ITemporaryProductMediaStorage
    {
        public int Stored; public string? LastId;
        public async Task StoreFileAsync(string containerId, string temporaryFileId, Stream content, CancellationToken cancellationToken = default)
        { using var bytes = new MemoryStream(); await content.CopyToAsync(bytes, cancellationToken); Assert.Equal(8, bytes.Length); Stored++; LastId = temporaryFileId; }
        public Task<Stream> OpenReadAsync(string containerId, string temporaryFileId, CancellationToken cancellationToken = default) => throw new FileNotFoundException();
        public Task DeleteAsync(string containerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class Validation(IProductMediaImportTokenService tokens, ProductMediaImportBatch batch) : IProductMediaImportValidationService
    {
        public Task<ProductMediaImportValidationResultDto> ValidateAsync(ProductMediaImportValidateCommand command, CancellationToken cancellationToken = default)
        {
            Assert.True(tokens.TryGetUpload(command.UploadToken, out var upload));
            Assert.Equal(batch.Id, upload.BatchId);
            batch.SetValidationSummary(1, 1, 0, 0);
            ProductMediaImportFileValidationDto file = new("original-item-id", "test.png", "TEST", Guid.NewGuid(), "TEST", "Test",
                ProductMediaMatchSource.FileName, 0, true, upload.Archive.Entries[0].Sha256, ProductMediaValidationFileStatus.Valid, [], []);
            var token = tokens.CreateValidation(new(batch.Id, command.UploadToken, new(), [file], DateTimeOffset.UtcNow.AddMinutes(30)));
            return Task.FromResult(new ProductMediaImportValidationResultDto(token, 1, 1, 0, 0, 0, 0, 1, 0, 0, [file], "ok"));
        }
    }
    private sealed class Execution(IProductMediaImportTokenService tokens, Storage storage) : IProductMediaImportExecutionService
    {
        public Task<ProductMediaImportExecutionResultDto> ExecuteAsync(ProductMediaImportExecuteCommand command, CancellationToken cancellationToken = default)
        {
            Assert.True(tokens.TryUseValidation(command.ValidationToken, out var validation));
            Assert.True(tokens.TryGetUpload(validation.UploadToken, out var upload));
            Assert.Equal(validation.BatchId, upload.BatchId);
            Assert.Equal("original-item-id", Assert.Single(validation.Files).TemporaryFileId);
            Assert.Equal(1, storage.Stored);
            return Task.FromResult(new ProductMediaImportExecutionResultDto(upload.BatchId, ProductMediaImportBatchStatus.Completed, 1, 0, 0, []));
        }
    }
}
