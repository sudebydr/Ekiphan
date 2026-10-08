using Ekiphan.Application.Catalog;

namespace Ekiphan.Infrastructure.Media;

public sealed class PublicMediaUrlResolver : IPublicMediaUrlResolver
{
    private readonly Uri? baseUri;
    private readonly bool useRelativeUrls;

    public PublicMediaUrlResolver(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            useRelativeUrls = true;
            return;
        }

        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttps && !IsLocalHttp(parsed)) ||
            string.IsNullOrWhiteSpace(parsed.Host) ||
            !string.IsNullOrEmpty(parsed.Query) ||
            !string.IsNullOrEmpty(parsed.Fragment))
        {
            return;
        }

        baseUri = parsed.AbsoluteUri.EndsWith('/')
            ? parsed
            : new Uri($"{parsed.AbsoluteUri}/", UriKind.Absolute);
    }

    public string? Resolve(string? storageKey)
    {
        if ((!useRelativeUrls && baseUri is null) || !IsSafeStorageKey(storageKey))
        {
            return null;
        }

        var escapedPath = string.Join(
            '/',
            storageKey!.Split('/').Select(Uri.EscapeDataString));

        if (baseUri is null)
        {
            return $"/{escapedPath}";
        }

        return new Uri(baseUri, escapedPath).AbsoluteUri;
    }

    private static bool IsSafeStorageKey(string? storageKey) =>
        !string.IsNullOrWhiteSpace(storageKey) &&
        storageKey.Length <= 500 &&
        !storageKey.StartsWith('/') &&
        !storageKey.Contains('\\') &&
        !storageKey.Contains("..", StringComparison.Ordinal) &&
        storageKey.All(
            character =>
                char.IsLetterOrDigit(character) ||
                character is '/' or '-' or '_' or '.');

    private static bool IsLocalHttp(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
}
