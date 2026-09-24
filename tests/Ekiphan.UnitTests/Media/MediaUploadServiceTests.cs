using System.Security.Cryptography;
using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;

namespace Ekiphan.UnitTests.Media;

public sealed class MediaUploadServiceTests
{
    [Fact]
    public async Task CleanValidatedFileIsStoredWithServerGeneratedKey()
    {
        var contentBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var storage = new StubStorage();
        var repository = new StubRepository();
        var service = CreateService(
            storage,
            repository,
            MediaThreatScanStatus.Clean);
        using var content = new MemoryStream(contentBytes);

        var result = await service.UploadAsync(
            new UploadMediaCommand(
                content,
                "product.jpg",
                "image/jpeg",
                content.Length,
                MediaAssetType.Image,
                "tr",
                "Ürün görseli",
                "Beyaz servis tabağı"));

        Assert.NotNull(repository.Asset);
        Assert.Equal(result.StorageKey, storage.SavedKey);
        Assert.Matches(
            "^media/2026/07/[0-9a-f]{32}\\.jpg$",
            result.StorageKey);
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(contentBytes)),
            result.Sha256Checksum);
    }

    [Fact]
    public async Task UnavailableThreatScannerFailsClosed()
    {
        var storage = new StubStorage();
        var service = CreateService(
            storage,
            new StubRepository(),
            MediaThreatScanStatus.Unavailable);
        using var content = Jpeg();

        await Assert.ThrowsAsync<MediaUploadUnavailableException>(
            () => service.UploadAsync(ImageCommand(content)));
        Assert.Null(storage.SavedKey);
    }

    [Fact]
    public async Task ThreatIsRejectedBeforeStorage()
    {
        var storage = new StubStorage();
        var service = CreateService(
            storage,
            new StubRepository(),
            MediaThreatScanStatus.ThreatFound);
        using var content = Jpeg();

        await Assert.ThrowsAsync<UnsafeMediaFileException>(
            () => service.UploadAsync(ImageCommand(content)));
        Assert.Null(storage.SavedKey);
    }

    [Fact]
    public async Task RepositoryFailureRollsBackStoredFile()
    {
        var storage = new StubStorage();
        var repository = new StubRepository
        {
            Failure = new DuplicateMediaContentException(),
        };
        var service = CreateService(
            storage,
            repository,
            MediaThreatScanStatus.Clean);
        using var content = Jpeg();

        await Assert.ThrowsAsync<DuplicateMediaContentException>(
            () => service.UploadAsync(ImageCommand(content)));
        Assert.Equal(storage.SavedKey, storage.DeletedKey);
    }

    private static MediaUploadService CreateService(
        StubStorage storage,
        StubRepository repository,
        MediaThreatScanStatus scanStatus) =>
        new(
            new MediaFileSignatureValidator(),
            new StubScanner(scanStatus),
            storage,
            repository,
            new FixedTimeProvider(
                new DateTimeOffset(
                    2026,
                    7,
                    29,
                    10,
                    0,
                    0,
                    TimeSpan.Zero)));

    private static MemoryStream Jpeg() =>
        new([0xFF, 0xD8, 0xFF, 0xE0]);

    private static UploadMediaCommand ImageCommand(MemoryStream content) =>
        new(
            content,
            "product.jpg",
            "image/jpeg",
            content.Length,
            MediaAssetType.Image,
            "tr",
            "Ürün görseli",
            "Beyaz servis tabağı");

    private sealed class StubScanner(MediaThreatScanStatus status)
        : IMediaThreatScanner
    {
        public Task<MediaThreatScanStatus> ScanAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(status);
    }

    private sealed class StubStorage : IMediaFileStorage
    {
        public bool IsConfigured => true;

        public string? SavedKey { get; private set; }

        public string? DeletedKey { get; private set; }

        public Task SaveAsync(
            string storageKey,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            SavedKey = storageKey;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            DeletedKey = storageKey;
            return Task.CompletedTask;
        }
    }

    private sealed class StubRepository : IMediaAssetRepository
    {
        public MediaAsset? Asset { get; private set; }

        public Exception? Failure { get; init; }

        public Task AddAsync(
            MediaAsset asset,
            CancellationToken cancellationToken = default)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Asset = asset;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
