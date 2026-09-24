using Ekiphan.Application.Content;
using Microsoft.Extensions.Logging;

namespace Ekiphan.Infrastructure.Content;

public sealed class UrlSafetyService : IUrlSafetyService
{
    public bool IsSafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;

        var trimmed = url.Trim();

        // Reject dangerous schemes
        if (trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Relative path or absolute URL check
        if (trimmed.StartsWith('/') || trimmed.StartsWith('#')) return true;

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        return false;
    }
}

public sealed class CmsCacheInvalidationService(ILogger<CmsCacheInvalidationService> logger) : ICmsCacheInvalidationService
{
    public Task InvalidateAsync(CmsCacheInvalidationRequest request, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Invalidating CMS Cache: References={References}, Showrooms={Showrooms}, Banners={Banners}, Logos={Logos}, Settings={Settings}, Footer={Footer}",
                request.References, request.Showrooms, request.Banners, request.CustomerLogos, request.Settings, request.Footer);
        }

        return Task.CompletedTask;
    }
}
