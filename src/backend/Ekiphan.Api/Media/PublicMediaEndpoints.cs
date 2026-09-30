using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Api.Media;

internal static class PublicMediaEndpoints
{
    private const string StorageKeyPrefix = "media/";

    public static IEndpointRouteBuilder MapPublicMediaEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods(
            "/media/{**path}",
            [HttpMethods.Get, HttpMethods.Head],
            GetAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        string? path,
        HttpRequest request,
        HttpResponse response,
        IWebHostEnvironment environment,
        EkiphanDbContext dbContext,
        IMediaFileStorage storage,
        CancellationToken cancellationToken)
    {
        if (!TryCreateStorageKey(path, out var storageKey))
        {
            return Results.NotFound();
        }

        var asset = await dbContext.MediaAssets
            .AsNoTracking()
            .Where(item =>
                item.StorageKey == storageKey &&
                item.Status == MediaStatus.Active &&
                (item.AssetType == MediaAssetType.Image || item.AssetType == MediaAssetType.Pdf) &&
                item.MimeType != null)
            .Select(item => new PublicMediaAsset(item.MimeType!, item.FileSizeBytes!.Value))
            .SingleOrDefaultAsync(cancellationToken);
        if (asset is null || !IsSupportedMimeType(asset.MimeType))
        {
            return Results.NotFound();
        }

        if (!TryGetRange(request.Headers.Range, asset.FileSizeBytes, out var start, out var end))
        {
            response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
            response.Headers.ContentRange = $"bytes */{asset.FileSizeBytes}";
            return Results.Empty;
        }

        var hasRange = start is not null;
        var content = await storage.OpenReadAsync(
            storageKey,
            hasRange ? $"bytes={start}-{end}" : null,
            cancellationToken);
        if (content is null)
        {
            return Results.NotFound();
        }

        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers.ContentDisposition = "inline";
        response.Headers["Content-Security-Policy"] = environment.IsDevelopment()
            ? "default-src 'none'; base-uri 'none'; frame-ancestors http://localhost:3000 http://127.0.0.1:3000; form-action 'none'"
            : "default-src 'none'; base-uri 'none'; frame-ancestors 'self'; form-action 'none'";
        response.Headers["Accept-Ranges"] = "bytes";
        if (hasRange)
        {
            var length = end!.Value - start!.Value + 1;
            response.StatusCode = StatusCodes.Status206PartialContent;
            response.Headers.ContentRange = $"bytes {start}-{end}/{asset.FileSizeBytes}";
            response.ContentLength = length;
            return Results.Stream(content, asset.MimeType);
        }

        return Results.File(content, asset.MimeType);
    }

    private static bool TryCreateStorageKey(string? path, out string storageKey)
    {
        storageKey = string.Empty;
        if (string.IsNullOrWhiteSpace(path) ||
            path.Length > 494 ||
            !(path.StartsWith(StorageKeyPrefix, StringComparison.OrdinalIgnoreCase) || path.StartsWith("catalogs/", StringComparison.OrdinalIgnoreCase)) ||
            path.StartsWith('/') ||
            path.Contains('\\') ||
            path.Contains("..", StringComparison.Ordinal) ||
            path.Split('/').Any(segment => string.IsNullOrEmpty(segment)) ||
            !path.All(character =>
                char.IsLetterOrDigit(character) ||
                character is '/' or '-' or '_' or '.'))
        {
            return false;
        }

        storageKey = path.StartsWith("catalogs/", StringComparison.OrdinalIgnoreCase) ? path : StorageKeyPrefix + path;
        return true;
    }

    private static bool IsSupportedMimeType(string mimeType) =>
        mimeType is "image/jpeg" or "image/png" or "image/webp" or "application/pdf";

    private static bool TryGetRange(
        string? rangeHeader,
        long length,
        out long? start,
        out long? end)
    {
        start = null;
        end = null;
        if (string.IsNullOrWhiteSpace(rangeHeader)) return true;
        if (!rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase)) return false;

        var parts = rangeHeader[6..].Split('-', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !long.TryParse(parts[0], out var requestedStart) ||
            requestedStart < 0 || requestedStart >= length) return false;
        var requestedEnd = string.IsNullOrEmpty(parts[1])
            ? length - 1
            : long.TryParse(parts[1], out var parsedEnd) ? parsedEnd : -1;
        if (requestedEnd < requestedStart) return false;

        start = requestedStart;
        end = Math.Min(requestedEnd, length - 1);
        return true;
    }

    private sealed record PublicMediaAsset(string MimeType, long FileSizeBytes);
}
