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
        HttpResponse response,
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
                item.AssetType == MediaAssetType.Image &&
                item.MimeType != null)
            .Select(item => new PublicMediaAsset(item.MimeType!))
            .SingleOrDefaultAsync(cancellationToken);
        if (asset is null || !IsSupportedImageMimeType(asset.MimeType))
        {
            return Results.NotFound();
        }

        var content = await storage.OpenReadAsync(storageKey, cancellationToken);
        if (content is null)
        {
            return Results.NotFound();
        }

        response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.File(content, asset.MimeType, enableRangeProcessing: true);
    }

    private static bool TryCreateStorageKey(string? path, out string storageKey)
    {
        storageKey = string.Empty;
        if (string.IsNullOrWhiteSpace(path) ||
            path.Length > 494 ||
            path.StartsWith(StorageKeyPrefix, StringComparison.OrdinalIgnoreCase) ||
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

        storageKey = StorageKeyPrefix + path;
        return true;
    }

    private static bool IsSupportedImageMimeType(string mimeType) =>
        mimeType is "image/jpeg" or "image/png" or "image/webp";

    private sealed record PublicMediaAsset(string MimeType);
}
