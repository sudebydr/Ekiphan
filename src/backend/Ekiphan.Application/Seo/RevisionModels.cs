using Ekiphan.Domain.Seo;

namespace Ekiphan.Application.Seo;

public sealed record SeoRevisionListItemDto(
    Guid Id,
    SeoEntityType EntityType,
    Guid EntityId,
    string LanguageCode,
    int VersionNumber,
    SeoChangeSource ChangeSource,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string? Reason);

public sealed record SeoRevisionDetailDto(
    Guid Id,
    SeoEntityType EntityType,
    Guid EntityId,
    string LanguageCode,
    int VersionNumber,
    string SnapshotJson,
    string? ChangedFieldsJson,
    SeoChangeSource ChangeSource,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    string? Reason,
    Guid? ImportBatchId);

public sealed record RestoreSeoRevisionCommand(
    Guid RevisionId,
    string? Reason = null);

public interface ISeoRevisionService
{
    Task<IReadOnlyList<SeoRevisionListItemDto>> GetRevisionsAsync(SeoEntityType entityType, Guid entityId, string languageCode, CancellationToken cancellationToken);
    Task<SeoRevisionDetailDto?> GetRevisionDetailAsync(Guid revisionId, CancellationToken cancellationToken);
    Task<bool> RestoreRevisionAsync(Guid revisionId, Guid actorUserId, string? reason, CancellationToken cancellationToken);
    Task RecordRevisionAsync(SeoEntityType entityType, Guid entityId, string languageCode, string snapshotJson, SeoChangeSource changeSource, Guid actorUserId, string? reason = null, Guid? importBatchId = null, CancellationToken cancellationToken = default);
}
