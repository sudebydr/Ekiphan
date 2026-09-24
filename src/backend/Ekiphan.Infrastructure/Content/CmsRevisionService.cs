using System.Text.Json;
using System.Text.Json.Serialization;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

public sealed class CmsSnapshotSerializer : ICmsSnapshotSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public string Serialize<T>(T entity) => JsonSerializer.Serialize(entity, Options);
    public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)!;
}

public sealed class CmsRevisionService(
    EkiphanDbContext dbContext,
    ICmsSnapshotSerializer serializer)
    : ICmsRevisionService
{
    public async Task<CmsRevision> CreateRevisionAsync(
        string entityType,
        Guid entityId,
        CmsRevisionChangeType changeType,
        Guid actorUserId,
        string? reason = null,
        string? changedFieldsJson = null,
        CancellationToken cancellationToken = default)
    {
        var type = entityType.Trim();
        var lastVersion = await dbContext.Set<CmsRevision>()
            .AsNoTracking()
            .Where(r => r.EntityType == type && r.EntityId == entityId)
            .MaxAsync(r => (int?)r.VersionNumber, cancellationToken) ?? 0;

        string snapshotJson = "{}";

        if (type.Equals("ReferenceProject", StringComparison.OrdinalIgnoreCase))
        {
            var entity = await dbContext.Set<ReferenceProject>().AsNoTracking().Include(x => x.Translations).Include(x => x.Media).Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == entityId, cancellationToken);
            if (entity is not null) snapshotJson = serializer.Serialize(entity);
        }
        else if (type.Equals("Showroom", StringComparison.OrdinalIgnoreCase))
        {
            var entity = await dbContext.Set<Showroom>().AsNoTracking().Include(x => x.Translations).Include(x => x.Media).Include(x => x.Hotspots).FirstOrDefaultAsync(x => x.Id == entityId, cancellationToken);
            if (entity is not null) snapshotJson = serializer.Serialize(entity);
        }
        else if (type.Equals("Banner", StringComparison.OrdinalIgnoreCase))
        {
            var entity = await dbContext.Set<Banner>().AsNoTracking().Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == entityId, cancellationToken);
            if (entity is not null) snapshotJson = serializer.Serialize(entity);
        }
        else if (type.Equals("SiteSetting", StringComparison.OrdinalIgnoreCase))
        {
            var settings = await dbContext.Set<SiteSetting>().AsNoTracking().Where(s => !s.IsSensitive).ToListAsync(cancellationToken);
            snapshotJson = serializer.Serialize(settings);
        }

        var rev = new CmsRevision(Guid.NewGuid(), type, entityId, lastVersion + 1, changeType, snapshotJson, changedFieldsJson, actorUserId, DateTimeOffset.UtcNow, reason);
        dbContext.Set<CmsRevision>().Add(rev);
        await dbContext.SaveChangesAsync(cancellationToken);
        return rev;
    }

    public async Task<IReadOnlyList<CmsRevisionListItemDto>> GetRevisionsAsync(string entityType, Guid entityId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var type = entityType.Trim();
        var p = page <= 0 ? 1 : page;
        var ps = pageSize <= 0 ? 20 : pageSize;

        return await dbContext.Set<CmsRevision>().AsNoTracking()
            .Where(r => r.EntityType == type && r.EntityId == entityId)
            .OrderByDescending(r => r.VersionNumber)
            .Skip((p - 1) * ps).Take(ps)
            .Select(r => new CmsRevisionListItemDto(
                r.Id, r.EntityType, r.EntityId, r.VersionNumber, r.ChangeType,
                r.CreatedByUserId, r.CreatedAt, r.Reason, r.SchemaVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<CmsRevisionDetailDto?> GetRevisionAsync(string entityType, Guid entityId, Guid revisionId, CancellationToken cancellationToken = default)
    {
        var type = entityType.Trim();
        var r = await dbContext.Set<CmsRevision>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == revisionId && x.EntityType == type && x.EntityId == entityId, cancellationToken);

        if (r is null) return null;
        return new CmsRevisionDetailDto(
            r.Id, r.EntityType, r.EntityId, r.VersionNumber, r.ChangeType,
            r.SnapshotJson, r.ChangedFieldsJson, r.CreatedByUserId, r.CreatedAt,
            r.Reason, r.CorrelationId, r.SchemaVersion);
    }
}

public sealed class CmsRevisionRestoreService(
    EkiphanDbContext dbContext,
    ICmsRevisionService revisionService,
    ICmsCacheInvalidationService cacheService)
    : ICmsRevisionRestoreService
{
    public async Task<ContentWorkflowResultDto> RestoreRevisionAsync(
        string entityType,
        Guid entityId,
        Guid revisionId,
        string reason,
        byte[] rowVersion,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var type = entityType.Trim();
        var rev = await dbContext.Set<CmsRevision>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == revisionId && r.EntityType == type && r.EntityId == entityId, cancellationToken);

        if (rev is null) return new ContentWorkflowResultDto(false, ContentWorkflowStatus.Draft, null, null, CmsManagementErrorCodes.CmsRevisionNotFound);

        if (type.Equals("ReferenceProject", StringComparison.OrdinalIgnoreCase))
        {
            var target = await dbContext.Set<ReferenceProject>().Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == entityId, cancellationToken);
            if (target is null) return new ContentWorkflowResultDto(false, ContentWorkflowStatus.Draft, null, null, CmsManagementErrorCodes.ReferenceNotFound);

            if (rowVersion.Length > 0 && target.RowVersion.Length > 0 && !target.RowVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

            await revisionService.CreateRevisionAsync(type, entityId, CmsRevisionChangeType.Restored, actorUserId, $"Pre-restore snapshot before restoring version {rev.VersionNumber}.", null, cancellationToken);

            target.SetWorkflowStatus(ContentWorkflowStatus.Draft, null, actorUserId);
            await dbContext.SaveChangesAsync(cancellationToken);
            await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(References: true), cancellationToken);

            return new ContentWorkflowResultDto(true, ContentWorkflowStatus.Draft, null, null, null);
        }
        else if (type.Equals("Showroom", StringComparison.OrdinalIgnoreCase))
        {
            var target = await dbContext.Set<Showroom>().Include(x => x.Translations).FirstOrDefaultAsync(x => x.Id == entityId, cancellationToken);
            if (target is null) return new ContentWorkflowResultDto(false, ContentWorkflowStatus.Draft, null, null, CmsManagementErrorCodes.ShowroomNotFound);

            if (rowVersion.Length > 0 && target.RowVersion.Length > 0 && !target.RowVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.CmsConcurrencyConflict);

            await revisionService.CreateRevisionAsync(type, entityId, CmsRevisionChangeType.Restored, actorUserId, $"Pre-restore snapshot before restoring version {rev.VersionNumber}.", null, cancellationToken);

            target.SetWorkflowStatus(ContentWorkflowStatus.Draft, null, actorUserId);
            await dbContext.SaveChangesAsync(cancellationToken);
            await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Showrooms: true), cancellationToken);

            return new ContentWorkflowResultDto(true, ContentWorkflowStatus.Draft, null, null, null);
        }

        return new ContentWorkflowResultDto(false, ContentWorkflowStatus.Draft, null, null, CmsManagementErrorCodes.CmsEntityNotFound);
    }
}
