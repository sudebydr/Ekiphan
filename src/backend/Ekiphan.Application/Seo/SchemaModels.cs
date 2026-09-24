using Ekiphan.Domain.Seo;

namespace Ekiphan.Application.Seo;

public sealed record SchemaMarkupResultDto(
    SeoEntityType EntityType,
    Guid EntityId,
    string LanguageCode,
    string SchemaType,
    string JsonLdContent);

public interface ISchemaMarkupService
{
    Task<SchemaMarkupResultDto> GenerateAsync(
        SeoEntityType entityType,
        Guid entityId,
        string languageCode,
        CancellationToken cancellationToken);
}
