using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Ekiphan.Api.Media;
using Xunit.Abstractions;

namespace Ekiphan.UnitTests.Media;

public sealed class ZipUploadLifetimeTests(ITestOutputHelper output) : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "zip-lifetime-" + Guid.NewGuid().ToString("N"));
    private readonly Guid owner = Guid.NewGuid();
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }

    [Fact]
    public async Task FiveActiveUploadsAreAllowedAndActivityExtendsIdleDeadline()
    {
        using var store = new ZipUploadStore(new() { Root = root });
        for (var i = 0; i < 5; i++) await store.CreateAsync(owner, "product", "test.zip", 1, i.ToString("X64", System.Globalization.CultureInfo.InvariantCulture), 1, default);
        Assert.Equal(5, (await store.ListAsync(owner, default)).Count);
        var state = (await store.ListAsync(owner, default))[0];
        var path = Path.Combine(Path.GetDirectoryName(store.ArchivePath(state.Id))!, "state.json");
        var full = await store.GetAsync(state.Id, owner, default);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(full with { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1) }));
        var renewed = await store.GetAsync(state.Id, owner, default);
        Assert.True(renewed.ExpiresAt > DateTimeOffset.UtcNow.AddDays(6));
    }

    [Theory]
    [InlineData("preview")]
    [InlineData("validate")]
    public async Task AwaitingApprovalSurvivesOldExpiryAndRestart(string operation)
    {
        using var store = new ZipUploadStore(new() { Root = root });
        var state = await store.CreateAsync(owner, "product", "test.zip", 1, new string('A', 64), 1, default);
        var path = Path.Combine(Path.GetDirectoryName(store.ArchivePath(state.Id))!, "state.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(state with { Phase = "completed", Operation = operation,
            Offset = 1, ExpiresAt = DateTimeOffset.UtcNow.AddDays(-2) }));
        using var restarted = new ZipUploadStore(new() { Root = root });
        await restarted.MaintainAsync(true, default);
        Assert.Single(await restarted.ListAsync(owner, default));
        Assert.Equal(operation, (await restarted.GetAsync(state.Id, owner, default)).Operation);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => restarted.GetAsync(state.Id, Guid.NewGuid(), default));
    }

    [Fact]
    public async Task SyntheticChunkBenchmarkKeepsHashAndDurabilityChecks()
    {
        // 32 MiB synthetic bytes, no ZIP import or application data. Report local disk cost, not WAN throughput.
        foreach (var mib in new[] { 1, 4, 8 })
        {
            var bytes = new byte[mib * 1024 * 1024];
            RandomNumberGenerator.Fill(bytes);
            var hashTime = TimeSpan.Zero;
            var watch = Stopwatch.StartNew();
            using var file = new FileStream(Path.Combine(EnsureRoot(), $"bench-{mib}"), FileMode.CreateNew, FileAccess.Write);
            for (var n = 0; n < 32 / mib; n++)
            {
                var hashWatch = Stopwatch.StartNew();
                _ = SHA256.HashData(bytes);
                hashTime += hashWatch.Elapsed;
                await file.WriteAsync(bytes);
                file.Flush(true);
            }
            output.WriteLine($"{mib} MiB: 32 MiB hash+durable-write {watch.Elapsed.TotalMilliseconds:F1} ms; hash {hashTime.TotalMilliseconds:F1} ms; requests/1.5GiB={1536 / mib}");
        }
    }
    private string EnsureRoot() { Directory.CreateDirectory(root); return root; }
}
