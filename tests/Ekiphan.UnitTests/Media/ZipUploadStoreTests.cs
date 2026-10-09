using System.Security.Cryptography;
using Ekiphan.Api.Media;

namespace Ekiphan.UnitTests.Media;

public sealed class ZipUploadStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ekiphan-upload-test-" + Guid.NewGuid().ToString("N"));
    private readonly Guid owner = Guid.NewGuid();
    private const string Fingerprint = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Theory]
    [InlineData("image", "test.webp")]
    [InlineData("pdf", "test.pdf")]
    public async Task SingleFilesPersistFieldsAndExecuteExactlyOnceWithoutArchivePreview(string kind, string fileName)
    {
        using var store = Store();
        var state = await store.CreateAsync(owner, kind, fileName, 1, Fingerprint, 1, default);
        var hash = Convert.ToHexString(SHA256.HashData(new byte[] { 1 }));
        await store.AppendAsync(state.Id, owner, 0, 1, hash, new MemoryStream([1]), default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.BeginAsync(state.Id, owner, "preview", default));
        Dictionary<string, string> fields = new() { ["title"] = "Original", ["altText"] = "Alt" };
        var processing = await store.BeginAsync(state.Id, owner, "execute", default, fields);
        Assert.Equal("Original", processing.Fields!["title"]);
        using var restarted = Store();
        Assert.Equal("Original", (await restarted.GetAsync(state.Id, owner, default)).Fields!["title"]);
        var replay = await store.BeginAsync(state.Id, owner, "execute", default, new Dictionary<string, string> { ["title"] = "Changed" });
        Assert.Equal("Original", replay.Fields!["title"]);
        await store.CompleteAsync(state.Id, owner, new { Id = Guid.NewGuid() }, null, default);
        Assert.Equal("completed", (await store.BeginAsync(state.Id, owner, "execute", default)).Phase);
        Assert.False(File.Exists(store.ArchivePath(state.Id)));
    }

    [Theory]
    [InlineData("product")]
    [InlineData("catalog")]
    public async Task RarUsesSameOwnerBoundSpoolAndChunkHashRules(string kind)
    {
        using var store = Store();
        var state = await store.CreateAsync(owner, kind, "test.rar", 1, Fingerprint, 1, default);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => store.GetAsync(state.Id, Guid.NewGuid(), default));
        await store.AppendAsync(state.Id, owner, 0, 1, Convert.ToHexString(SHA256.HashData(new byte[] { 1 })), new MemoryStream([1]), default);
        Assert.Equal("processing", (await store.BeginAsync(state.Id, owner, "preview", default)).Phase);
    }
    [Fact]
    public async Task SlowClientDoesNotBlockStatusReadsAndParallelChunkReplayRemainsSingle()
    {
        using var store = Store();
        var state = await store.CreateAsync(owner, "catalog", "test.zip", 1, Fingerprint, 1, default);
        var hash = Convert.ToHexString(SHA256.HashData(new byte[] { 1 }));
        using var slow = new SlowStream();
        var first = store.AppendAsync(state.Id, owner, 0, 1, hash, slow, default);
        await slow.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var busy = await Assert.ThrowsAsync<ZipUploadException>(() => store.DeleteAsync(state.Id, owner, default));
        Assert.Equal("UPLOAD_BUSY", busy.Code);
        Assert.Equal(0, (await store.GetAsync(state.Id, owner, default).WaitAsync(TimeSpan.FromSeconds(1))).Offset);
        slow.Release.SetResult();
        await Task.WhenAll(first, store.AppendAsync(state.Id, owner, 0, 1, hash, new MemoryStream([1]), default));
        Assert.Single((await store.GetAsync(state.Id, owner, default)).Chunks!);
    }

    private sealed class SlowStream() : MemoryStream(new byte[] { 1 })
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return await base.ReadAsync(buffer, cancellationToken);
        }
    }
    private ZipUploadStore Store(long quota = 32L * 1024 * 1024 * 1024) => new(new() { Root = root, ReservedBytesLimit = quota });
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }

    [Theory]
    [InlineData(875L * 1024 * 1024)]
    [InlineData(1024L * 1024 * 1024)]
    [InlineData(1536L * 1024 * 1024)]
    public async Task LargeFileReservationsDoNotAllocateOrCopyTheArchive(long length)
    {
        using var store = Store();
        var state = await store.CreateAsync(owner, "catalog", "test.zip", length, Fingerprint, ZipUploadStore.CatalogMaximumBytes, default);
        Assert.Equal(length, state.Length);
        Assert.Equal(0, state.Offset);
        Assert.False(File.Exists(store.ArchivePath(state.Id)));
    }

    [Fact]
    public async Task InterruptedChunkDoesNotAdvanceAndResumeSurvivesStoreRestart()
    {
        using var store = Store();
        var bytes = new byte[] { 1, 2, 3, 4 };
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var state = await store.CreateAsync(owner, "product", "test.zip", 8, Fingerprint, 8, default);
        await Assert.ThrowsAsync<ArgumentException>(() => store.AppendAsync(state.Id, owner, 0, 4, hash, new MemoryStream(bytes[..2]), default));
        Assert.Equal(0, (await store.GetAsync(state.Id, owner, default)).Offset);
        await store.AppendAsync(state.Id, owner, 0, 4, hash, new MemoryStream(bytes), default);
        using var restarted = Store();
        Assert.Equal(4, (await restarted.GetAsync(state.Id, owner, default)).Offset);
        await restarted.AppendAsync(state.Id, owner, 4, 4, hash, new MemoryStream(bytes), default);
        Assert.Equal(bytes.Concat(bytes), await File.ReadAllBytesAsync(store.ArchivePath(state.Id)));
    }

    [Fact]
    public async Task LostAcknowledgementReplayIsIdempotentAndDifferentContentCannotOverwrite()
    {
        using var store = Store();
        var bytes = new byte[] { 1, 2, 3 };
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var state = await store.CreateAsync(owner, "catalog", "test.zip", 3, Fingerprint, 3, default);
        await store.AppendAsync(state.Id, owner, 0, 3, hash, new MemoryStream(bytes), default);
        var replay = await store.AppendAsync(state.Id, owner, 0, 3, hash, new MemoryStream(bytes), default);
        Assert.Equal(3, replay.Offset);
        Assert.Single(replay.Chunks!);
        var different = new byte[] { 4, 5, 6 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.AppendAsync(state.Id, owner, 0, 3,
            Convert.ToHexString(SHA256.HashData(different)), new MemoryStream(different), default));
    }

    [Fact]
    public async Task HashMismatchOversizeAndForeignOwnerAreRejected()
    {
        using var store = Store();
        var state = await store.CreateAsync(owner, "catalog", "test.zip", 3, Fingerprint, 3, default);
        await Assert.ThrowsAsync<ArgumentException>(() => store.AppendAsync(state.Id, owner, 0, 3, Fingerprint, new MemoryStream([1, 2, 3]), default));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => store.GetAsync(state.Id, Guid.NewGuid(), default));
        await Assert.ThrowsAsync<ArgumentException>(() => store.CreateAsync(owner, "catalog", "test.zip", 4, Fingerprint, 3, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.BeginAsync(state.Id, owner, "preview", default));
    }

    [Fact]
    public async Task ProcessingRetriesReturnSavedResultAndRestartNeverReexecutesPersistence()
    {
        using var store = Store();
        var state = await store.CreateAsync(owner, "catalog", "../test.zip", 1, Fingerprint, 1, default);
        Assert.Equal("test.zip", state.FileName);
        await store.AppendAsync(state.Id, owner, 0, 1, Convert.ToHexString(SHA256.HashData(new byte[] { 1 })), new MemoryStream([1]), default);
        await store.BeginAsync(state.Id, owner, "preview", default);
        await store.CompleteAsync(state.Id, owner, new { PdfCount = 1 }, null, default);
        Assert.Equal("completed", (await store.BeginAsync(state.Id, owner, "preview", default)).Phase);
        await store.BeginAsync(state.Id, owner, "execute", default);
        await store.MaintainAsync(true, default);
        Assert.Equal("failed", (await store.BeginAsync(state.Id, owner, "execute", default)).Phase);
    }

    [Fact]
    public async Task DiskReservationQuotaIsEnforced()
    {
        using var store = Store(3);
        await store.CreateAsync(owner, "catalog", "test.zip", 3, Fingerprint, 3, default);
        var error = await Assert.ThrowsAsync<ZipUploadException>(() => store.CreateAsync(owner, "catalog", "other.zip", 1, Fingerprint, 3, default));
        Assert.Equal("UPLOAD_DISK_QUOTA", error.Code);
    }

    [Fact]
    public async Task FivePreviewsRemainAvailableWithoutConsumingActiveUserQuota()
    {
        using var store = Store();
        var previews = new List<ZipUploadState>();
        for (var number = 1; number <= 5; number++) previews.Add(await ReadyAsync(store, number));
        await store.MaintainAsync(false, default);
        Assert.Equal(5, (await store.ListAsync(owner, default)).Count);
        foreach (var preview in previews)
        {
            Assert.True(File.Exists(store.ArchivePath(preview.Id)));
            Assert.Equal("completed", (await store.BeginAsync(preview.Id, owner, "preview", default)).Phase);
        }
        for (var number = 6; number <= 8; number++)
            await store.CreateAsync(owner, "product", $"{number}.zip", 1, number.ToString("X64", System.Globalization.CultureInfo.InvariantCulture), 1, default);
        await store.CreateAsync(owner, "product", "ninth.zip", 1, 9.ToString("X64", System.Globalization.CultureInfo.InvariantCulture), 1, default);
        Assert.Equal(9, (await store.ListAsync(owner, default)).Count);
    }

    [Fact]
    public async Task ManualCloseIsOwnerBoundIdempotentAndInvalidatesOnlyThatPreview()
    {
        using var store = Store();
        var first = await ReadyAsync(store, 1);
        var second = await ReadyAsync(store, 2);
        Assert.Empty(await store.ListAsync(Guid.NewGuid(), default));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => store.DeleteAsync(first.Id, Guid.NewGuid(), default));
        await store.DeleteAsync(first.Id, owner, default);
        await store.DeleteAsync(first.Id, owner, default);
        Assert.False(File.Exists(store.ArchivePath(first.Id)));
        Assert.Equal("closed", (await store.GetAsync(first.Id, owner, default)).Phase);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.BeginAsync(first.Id, owner, "execute", default));
        Assert.Equal(second.Id, Assert.Single(await store.ListAsync(owner, default)).Id);
        Assert.True(File.Exists(store.ArchivePath(second.Id)));
        var fresh = await store.CreateAsync(owner, "product", "1.zip", 1, 1.ToString("X64", System.Globalization.CultureInfo.InvariantCulture), 1, default);
        Assert.NotEqual(first.Id, fresh.Id);
    }

    [Fact]
    public async Task CompletedImportReleasesArchiveAndQuotaButKeepsIdempotencyReceipt()
    {
        using var store = new ZipUploadStore(new() { Root = root, ReservedBytesLimit = 1 });
        for (var number = 1; number <= 5; number++)
        {
            var state = await ReadyAsync(store, number);
            await store.BeginAsync(state.Id, owner, "execute", default);
            await store.CompleteAsync(state.Id, owner, new { ImportedFiles = 1 }, null, default);
            Assert.False(File.Exists(store.ArchivePath(state.Id)));
            Assert.Empty(await store.ListAsync(owner, default));
            await store.DeleteAsync(state.Id, owner, default);
            var replay = await store.BeginAsync(state.Id, owner, "execute", default);
            Assert.Equal("completed", replay.Phase);
            Assert.Equal(1, replay.Result!.Value.GetProperty("importedFiles").GetInt32());
            Assert.Equal(state.Id, (await store.CreateAsync(owner, "catalog", $"{number}.zip", 1, number.ToString("X64", System.Globalization.CultureInfo.InvariantCulture), 1, default)).Id);
        }
    }

    [Fact]
    public async Task ProcessingCannotBeClosedAndRetainedCapacityHasSeparateReason()
    {
        using var store = new ZipUploadStore(new() { Root = root, ReservedBytesLimit = 1 });
        var state = await ReadyAsync(store, 1);
        var quota = await Assert.ThrowsAsync<ZipUploadException>(() => store.CreateAsync(Guid.NewGuid(), "product", "2.zip", 1, 2.ToString("X64", System.Globalization.CultureInfo.InvariantCulture), 1, default));
        Assert.Equal("UPLOAD_DISK_QUOTA", quota.Code);
        await store.BeginAsync(state.Id, owner, "execute", default);
        Assert.False(Assert.Single(await store.ListAsync(owner, default)).CanClose);
        var busy = await Assert.ThrowsAsync<ZipUploadException>(() => store.DeleteAsync(state.Id, owner, default));
        Assert.Equal("UPLOAD_BUSY", busy.Code);
        Assert.True(File.Exists(store.ArchivePath(state.Id)));
    }

    [Fact]
    public async Task ExpiredUnprocessedSessionIsCleanedBeforeAdmissionWithoutWaitingForTimer()
    {
        using var store = Store(1);
        var state = await store.CreateAsync(owner, "product", "1.zip", 1, Fingerprint, 1, default);
        var metadata = Path.Combine(Path.GetDirectoryName(store.ArchivePath(state.Id))!, "state.json");
        await File.WriteAllTextAsync(metadata, System.Text.Json.JsonSerializer.Serialize(state with { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) }));
        var next = await store.CreateAsync(owner, "product", "2.zip", 1, 2.ToString("X64", System.Globalization.CultureInfo.InvariantCulture), 1, default);
        Assert.NotEqual(state.Id, next.Id);
        Assert.False(File.Exists(metadata));
    }

    private async Task<ZipUploadState> ReadyAsync(ZipUploadStore store, int number)
    {
        var state = await store.CreateAsync(owner, "catalog", $"{number}.zip", 1, number.ToString("X64", System.Globalization.CultureInfo.InvariantCulture), 1, default);
        await store.AppendAsync(state.Id, owner, 0, 1, Convert.ToHexString(SHA256.HashData(new byte[] { 1 })), new MemoryStream([1]), default);
        await store.BeginAsync(state.Id, owner, "preview", default);
        await store.CompleteAsync(state.Id, owner, new { UploadToken = "synthetic" }, null, default);
        return state;
    }

    [Fact]
    public async Task OrphanCleanupOnlyRemovesOldGuidFoldersAndLeavesRecentAndUnrelatedFolders()
    {
        using var store = Store();
        await store.CreateAsync(owner, "product", "one.zip", 1, Fingerprint, 1, default);
        var old = Path.Combine(root, Guid.NewGuid().ToString("N"));
        var recent = Path.Combine(root, Guid.NewGuid().ToString("N"));
        var unrelated = Path.Combine(root, "unrelated");
        foreach (var folder in new[] { old, recent, unrelated }) Directory.CreateDirectory(folder);
        await File.WriteAllBytesAsync(Path.Combine(old, "archive.zip"), [1]);
        Directory.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddHours(-25));
        Directory.SetLastWriteTimeUtc(unrelated, DateTime.UtcNow.AddHours(-25));
        await store.MaintainAsync(false, default);
        Assert.False(Directory.Exists(old));
        Assert.True(Directory.Exists(recent));
        Assert.True(Directory.Exists(unrelated));
    }

    [Fact]
    public async Task CleanupDefersLockedArchiveWithoutLosingCompletedImportReceipt()
    {
        using var store = Store();
        var state = await ReadyAsync(store, 1);
        await store.BeginAsync(state.Id, owner, "execute", default);
        using (var locked = new FileStream(store.ArchivePath(state.Id), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await store.CompleteAsync(state.Id, owner, new { Imported = 1 }, null, default);
            var completed = await store.GetAsync(state.Id, owner, default);
            Assert.Equal("completed", completed.Phase);
            Assert.Equal(1, completed.Result!.Value.GetProperty("imported").GetInt32());
            if (OperatingSystem.IsWindows()) Assert.False(completed.ArchiveReleased);
        }
        await store.MaintainAsync(false, default);
        Assert.True((await store.GetAsync(state.Id, owner, default)).ArchiveReleased);
        Assert.False(File.Exists(store.ArchivePath(state.Id)));
    }
}
