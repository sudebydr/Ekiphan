using Ekiphan.Application.Media;

namespace Ekiphan.Infrastructure.Media;

public sealed class LocalMediaFileStorage : IMediaFileStorage
{
    private readonly string? rootPath;
    private readonly string? publicBaseUrl;

    public LocalMediaFileStorage(string? configuredRoot, string? configuredPublicBaseUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            rootPath = Path.GetFullPath(configuredRoot.Trim())
                .TrimEnd(Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
        }
        publicBaseUrl = configuredPublicBaseUrl?.Trim().TrimEnd('/');
    }

    public bool IsConfigured => rootPath is not null;

    public string ProviderName => "Local";

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var targetPath = ResolvePath(storageKey);
        var directory = Path.GetDirectoryName(targetPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath =
            $"{targetPath}.upload-{Guid.NewGuid():N}";
        try
        {
            await using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81_920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await content.CopyToAsync(
                    destination,
                    cancellationToken);
                await destination.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, targetPath, overwrite: false);
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var targetPath = ResolvePath(storageKey);
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81_920, FileOptions.Asynchronous)
            : null;
        return Task.FromResult(stream);
    }

    public Task MoveAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = ResolvePath(sourceKey);
        var destination = ResolvePath(destinationKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Move(source, destination, false);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storageKey) => publicBaseUrl is null
        ? "/" + storageKey.TrimStart('/')
        : $"{publicBaseUrl}/{storageKey.TrimStart('/')}";

    private string ResolvePath(string storageKey)
    {
        if (rootPath is null)
        {
            throw new MediaUploadUnavailableException(
                "Local media storage is not configured.");
        }

        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Length > 500 ||
            storageKey.StartsWith('/') ||
            storageKey.Contains('\\') ||
            storageKey.Contains("..", StringComparison.Ordinal) ||
            storageKey.Any(
                character =>
                    !char.IsLetterOrDigit(character) &&
                    character is not '/' and not '-' and not '_' and not '.'))
        {
            throw new UnsafeMediaFileException(
                "The media storage key is unsafe.");
        }

        var candidate = Path.GetFullPath(
            Path.Combine(
                rootPath,
                storageKey.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(
                rootPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnsafeMediaFileException(
                "The media storage path escapes its configured root.");
        }

        return candidate;
    }
}
