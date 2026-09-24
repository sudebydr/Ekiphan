using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ekiphan.Infrastructure.Seo;

public sealed class SchemaMarkupService : ISchemaMarkupService
{
    private readonly IEnumerable<ISeoDocumentProvider> _documentProviders;
    private readonly EkiphanDbContext _dbContext;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public SchemaMarkupService(
        IEnumerable<ISeoDocumentProvider> documentProviders,
        EkiphanDbContext dbContext,
        IConfiguration configuration)
    {
        _documentProviders = documentProviders;
        _dbContext = dbContext;
        _baseUrl = (configuration["SeoSettings:BaseUrl"] ?? "https://ekiphan.com").TrimEnd('/');
        _jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
    }

    public async Task<SchemaMarkupResultDto> GenerateAsync(SeoEntityType entityType, Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var provider = _documentProviders.FirstOrDefault(p => p.EntityType == entityType);
        if (provider == null)
        {
            return new SchemaMarkupResultDto(entityType, entityId, languageCode, "Thing", "{}");
        }

        var doc = await provider.GetAsync(entityId, languageCode, cancellationToken);
        if (doc == null)
        {
            return new SchemaMarkupResultDto(entityType, entityId, languageCode, "Thing", "{}");
        }

        object schemaObj = entityType switch
        {
            SeoEntityType.Product => BuildProductSchema(doc),
            SeoEntityType.NewsArticle => BuildArticleSchema(doc),
            _ => BuildWebPageSchema(doc)
        };

        string jsonLd = JsonSerializer.Serialize(schemaObj, _jsonOptions);
        jsonLd = jsonLd.Replace("</script>", "\\u003C/script\\u003E", StringComparison.OrdinalIgnoreCase);

        return new SchemaMarkupResultDto(entityType, entityId, languageCode, entityType.ToString(), jsonLd);
    }

    private object BuildProductSchema(SeoDocument doc) => new
    {
        context = "https://schema.org",
        type = "Product",
        name = doc.MetaTitle ?? doc.Slug,
        description = doc.MetaDescription,
        url = $"{_baseUrl}{doc.Url}"
    };

    private object BuildArticleSchema(SeoDocument doc) => new
    {
        context = "https://schema.org",
        type = "NewsArticle",
        headline = doc.MetaTitle ?? doc.Slug,
        description = doc.MetaDescription,
        url = $"{_baseUrl}{doc.Url}",
        datePublished = doc.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture),
        dateModified = doc.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture)
    };

    private object BuildWebPageSchema(SeoDocument doc) => new
    {
        context = "https://schema.org",
        type = "WebPage",
        name = doc.MetaTitle ?? doc.Slug,
        description = doc.MetaDescription,
        url = $"{_baseUrl}{doc.Url}"
    };
}
