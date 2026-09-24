using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Ekiphan.Api.Seo;

internal static class PublicSeoEndpoints
{
    public static void MapPublicSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/sitemap.xml", async (ISitemapService sitemapService, CancellationToken ct) =>
        {
            var result = await sitemapService.GenerateMainAsync(ct);
            return Results.Content(result.Content, result.ContentType);
        });

        endpoints.MapGet("/sitemap-index.xml", async (ISitemapService sitemapService, CancellationToken ct) =>
        {
            var result = await sitemapService.GenerateIndexAsync(ct);
            return Results.Content(result.Content, result.ContentType);
        });

        endpoints.MapGet("/sitemaps/{section}-{languageCode}.xml", async (string section, string languageCode, ISitemapService sitemapService, CancellationToken ct) =>
        {
            if (!Enum.TryParse<SeoEntityType>(section, true, out var entityType))
            {
                entityType = SeoEntityType.Product;
            }

            var result = await sitemapService.GenerateSectionAsync(new SitemapSectionRequest(entityType, languageCode), ct);
            return Results.Content(result.Content, result.ContentType);
        });

        endpoints.MapGet("/robots.txt", async (IRobotsConfigurationService robotsService, CancellationToken ct) =>
        {
            var content = await robotsService.GetPublicRobotsTxtAsync("Production", ct);
            return Results.Content(content, "text/plain");
        });

        endpoints.MapGet("/api/public/seo/schema/{entityType}/{entityId:guid}", async (string entityType, Guid entityId, string? lang, ISchemaMarkupService schemaService, CancellationToken ct) =>
        {
            if (!Enum.TryParse<SeoEntityType>(entityType, true, out var parsedType))
            {
                return Results.BadRequest("Invalid entity type.");
            }

            var result = await schemaService.GenerateAsync(parsedType, entityId, lang ?? "tr", ct);
            return Results.Content(result.JsonLdContent, "application/ld+json");
        });
    }
}
