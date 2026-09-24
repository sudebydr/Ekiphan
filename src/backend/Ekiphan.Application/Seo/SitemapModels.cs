using Ekiphan.Domain.Seo;

namespace Ekiphan.Application.Seo;

public sealed record SitemapUrlDto(
    string Loc,
    DateTimeOffset? LastMod,
    string? ChangeFreq = null,
    double? Priority = null);

public sealed record SitemapIndexItemDto(
    string Loc,
    DateTimeOffset? LastMod);

public sealed record SitemapResult(
    string Content,
    string ContentType = "application/xml",
    string? ETag = null,
    DateTimeOffset? LastModified = null);

public sealed record SitemapSectionRequest(
    SeoEntityType Section,
    string LanguageCode,
    int Page = 1,
    int PageSize = 50000);

public interface ISitemapService
{
    Task<SitemapResult> GenerateMainAsync(CancellationToken cancellationToken);
    Task<SitemapResult> GenerateIndexAsync(CancellationToken cancellationToken);
    Task<SitemapResult> GenerateSectionAsync(SitemapSectionRequest request, CancellationToken cancellationToken);
}

public interface ISitemapUrlProvider
{
    Task<IReadOnlyCollection<SitemapUrlDto>> GetUrlsAsync(SitemapSectionRequest request, CancellationToken cancellationToken);
}

public interface ISeoCacheInvalidationService
{
    Task InvalidateForEntityAsync(SeoEntityType entityType, Guid entityId, CancellationToken cancellationToken);
    Task InvalidateAllAsync(CancellationToken cancellationToken);
}
