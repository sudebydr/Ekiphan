using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Ekiphan.Infrastructure.Seo;

public sealed class SitemapService : ISitemapService
{
    private readonly IEnumerable<ISeoDocumentProvider> _documentProviders;
    private readonly IMemoryCache _memoryCache;
    private readonly string _baseUrl;

    public SitemapService(
        IEnumerable<ISeoDocumentProvider> documentProviders,
        IMemoryCache memoryCache,
        IConfiguration configuration)
    {
        _documentProviders = documentProviders;
        _memoryCache = memoryCache;
        _baseUrl = (configuration["SeoSettings:BaseUrl"] ?? "https://ekiphan.com").TrimEnd('/');
    }

    public async Task<SitemapResult> GenerateMainAsync(CancellationToken cancellationToken)
    {
        var cacheKey = "seo:sitemap:main";
        if (_memoryCache.TryGetValue(cacheKey, out SitemapResult? cached) && cached != null)
        {
            return cached;
        }

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Async = true,
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = false
        };

        using (var writer = XmlWriter.Create(sb, settings))
        {
            await writer.WriteStartDocumentAsync();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (var provider in _documentProviders)
            {
                await foreach (var doc in provider.StreamAllAsync("tr", cancellationToken))
                {
                    if (!doc.IsPublished || doc.IsArchived || doc.NoIndex) continue;

                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", $"{_baseUrl}{doc.Url}");
                    if (doc.UpdatedAt.HasValue)
                    {
                        writer.WriteElementString("lastmod", doc.UpdatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture));
                    }
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
            await writer.WriteEndDocumentAsync();
        }

        var xml = sb.ToString();
        var etag = ComputeHash(xml);
        var result = new SitemapResult(xml, "application/xml", etag, DateTimeOffset.UtcNow);

        _memoryCache.Set(cacheKey, result, TimeSpan.FromHours(1));
        return result;
    }

    public async Task<SitemapResult> GenerateIndexAsync(CancellationToken cancellationToken)
    {
        var cacheKey = "seo:sitemap:index";
        if (_memoryCache.TryGetValue(cacheKey, out SitemapResult? cached) && cached != null)
        {
            return cached;
        }

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Async = true, Encoding = Encoding.UTF8, Indent = true };

        using (var writer = XmlWriter.Create(sb, settings))
        {
            await writer.WriteStartDocumentAsync();
            writer.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

            var sections = new[] { "products", "categories", "brands", "content", "news", "references", "showrooms" };
            foreach (var section in sections)
            {
                writer.WriteStartElement("sitemap");
                writer.WriteElementString("loc", $"{_baseUrl}/sitemaps/{section}-tr.xml");
                writer.WriteElementString("lastmod", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture));
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            await writer.WriteEndDocumentAsync();
        }

        var xml = sb.ToString();
        var etag = ComputeHash(xml);
        var result = new SitemapResult(xml, "application/xml", etag, DateTimeOffset.UtcNow);

        _memoryCache.Set(cacheKey, result, TimeSpan.FromHours(1));
        return result;
    }

    public async Task<SitemapResult> GenerateSectionAsync(SitemapSectionRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"seo:sitemap:{request.Section}:{request.LanguageCode}:{request.Page}";
        if (_memoryCache.TryGetValue(cacheKey, out SitemapResult? cached) && cached != null)
        {
            return cached;
        }

        var provider = _documentProviders.FirstOrDefault(p => p.EntityType == request.Section);
        if (provider == null)
        {
            return new SitemapResult("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" />");
        }

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Async = true, Encoding = Encoding.UTF8, Indent = true };

        using (var writer = XmlWriter.Create(sb, settings))
        {
            await writer.WriteStartDocumentAsync();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            int skipped = (request.Page - 1) * request.PageSize;
            int count = 0;
            int processed = 0;

            await foreach (var doc in provider.StreamAllAsync(request.LanguageCode, cancellationToken))
            {
                if (!doc.IsPublished || doc.IsArchived || doc.NoIndex) continue;

                processed++;
                if (processed <= skipped) continue;
                if (count >= request.PageSize) break;

                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{_baseUrl}{doc.Url}");
                if (doc.UpdatedAt.HasValue)
                {
                    writer.WriteElementString("lastmod", doc.UpdatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture));
                }
                writer.WriteEndElement();
                count++;
            }

            writer.WriteEndElement();
            await writer.WriteEndDocumentAsync();
        }

        var xml = sb.ToString();
        var etag = ComputeHash(xml);
        var result = new SitemapResult(xml, "application/xml", etag, DateTimeOffset.UtcNow);

        _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
        return result;
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return $"\"{Convert.ToHexString(bytes)[..16]}\"";
    }
}

public sealed class SeoCacheInvalidationService : ISeoCacheInvalidationService
{
    private readonly IMemoryCache _memoryCache;

    public SeoCacheInvalidationService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task InvalidateForEntityAsync(SeoEntityType entityType, Guid entityId, CancellationToken cancellationToken)
    {
        _memoryCache.Remove("seo:sitemap:main");
        _memoryCache.Remove("seo:sitemap:index");
        _memoryCache.Remove($"seo:sitemap:{entityType.ToString().ToLowerInvariant()}:tr:1");
        _memoryCache.Remove($"seo:sitemap:{entityType.ToString().ToLowerInvariant()}:en:1");
        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync(CancellationToken cancellationToken)
    {
        _memoryCache.Remove("seo:sitemap:main");
        _memoryCache.Remove("seo:sitemap:index");
        return Task.CompletedTask;
    }
}
