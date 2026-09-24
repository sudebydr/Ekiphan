using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo;

public sealed class SeoRevisionService : ISeoRevisionService
{
    private readonly EkiphanDbContext _dbContext;

    public SeoRevisionService(EkiphanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SeoRevisionListItemDto>> GetRevisionsAsync(SeoEntityType entityType, Guid entityId, string languageCode, CancellationToken cancellationToken)
    {
        var revisions = await _dbContext.SeoRevisions
            .AsNoTracking()
            .Where(r => r.EntityType == entityType && r.EntityId == entityId && r.LanguageCode == languageCode)
            .OrderByDescending(r => r.VersionNumber)
            .ToListAsync(cancellationToken);

        return revisions.Select(r => new SeoRevisionListItemDto(
            r.Id, r.EntityType, r.EntityId, r.LanguageCode, r.VersionNumber, r.ChangeSource, r.CreatedByUserId, r.CreatedAt, r.Reason)).ToList();
    }

    public async Task<SeoRevisionDetailDto?> GetRevisionDetailAsync(Guid revisionId, CancellationToken cancellationToken)
    {
        var r = await _dbContext.SeoRevisions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == revisionId, cancellationToken);
        if (r == null) return null;

        return new SeoRevisionDetailDto(
            r.Id, r.EntityType, r.EntityId, r.LanguageCode, r.VersionNumber, r.SnapshotJson, r.ChangedFieldsJson, r.ChangeSource, r.CreatedByUserId, r.CreatedAt, r.Reason, r.ImportBatchId);
    }

    public async Task<bool> RestoreRevisionAsync(Guid revisionId, Guid actorUserId, string? reason, CancellationToken cancellationToken)
    {
        var revision = await _dbContext.SeoRevisions.FirstOrDefaultAsync(r => r.Id == revisionId, cancellationToken);
        if (revision == null) return false;

        // Record a new snapshot before restoration
        var maxVersion = await _dbContext.SeoRevisions
            .Where(r => r.EntityType == revision.EntityType && r.EntityId == revision.EntityId && r.LanguageCode == revision.LanguageCode)
            .MaxAsync(r => (int?)r.VersionNumber, cancellationToken) ?? 0;

        var restoredRevision = new SeoRevision(
            Guid.NewGuid(),
            revision.EntityType,
            revision.EntityId,
            revision.LanguageCode,
            maxVersion + 1,
            revision.SnapshotJson,
            "Restored previous snapshot",
            SeoChangeSource.Rollback,
            actorUserId,
            reason ?? $"Restored from revision {revision.VersionNumber}");

        _dbContext.SeoRevisions.Add(restoredRevision);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RecordRevisionAsync(SeoEntityType entityType, Guid entityId, string languageCode, string snapshotJson, SeoChangeSource changeSource, Guid actorUserId, string? reason = null, Guid? importBatchId = null, CancellationToken cancellationToken = default)
    {
        var maxVersion = await _dbContext.SeoRevisions
            .Where(r => r.EntityType == entityType && r.EntityId == entityId && r.LanguageCode == languageCode)
            .MaxAsync(r => (int?)r.VersionNumber, cancellationToken) ?? 0;

        var rev = new SeoRevision(
            Guid.NewGuid(),
            entityType,
            entityId,
            languageCode,
            maxVersion + 1,
            snapshotJson,
            null,
            changeSource,
            actorUserId,
            reason,
            importBatchId);

        _dbContext.SeoRevisions.Add(rev);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
