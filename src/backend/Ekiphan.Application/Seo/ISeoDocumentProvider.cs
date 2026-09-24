using Ekiphan.Domain.Seo;

namespace Ekiphan.Application.Seo;

public sealed record SeoDocument
{
    public required SeoEntityType EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required string LanguageCode { get; init; }
    public required string Slug { get; init; }
    public required string Url { get; init; }

    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? CanonicalUrl { get; init; }

    public string? OpenGraphTitle { get; init; }
    public string? OpenGraphDescription { get; init; }
    public Guid? OpenGraphMediaAssetId { get; init; }

    public bool NoIndex { get; init; }
    public bool NoFollow { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public bool IsPublished { get; init; }
    public bool IsArchived { get; init; }
}

public interface ISeoDocumentProvider
{
    SeoEntityType EntityType { get; }

    Task<SeoDocument?> GetAsync(
        Guid entityId,
        string languageCode,
        CancellationToken cancellationToken);

    IAsyncEnumerable<SeoDocument> StreamAllAsync(
        string languageCode,
        CancellationToken cancellationToken);
}
