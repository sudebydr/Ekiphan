using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Media;

internal sealed class MediaProcessingRepository(EkiphanDbContext db) : IMediaProcessingRepository
{
    public Task<MediaAsset?> FindReusableByHashAsync(string hash, CancellationToken cancellationToken) =>
        db.MediaAssets.Include(x => x.Variants).FirstOrDefaultAsync(x =>
            x.Sha256Checksum == hash && x.Status == MediaStatus.Active &&
            x.ProcessingStatus == MediaProcessingStatus.Completed, cancellationToken);

    public Task<MediaAsset?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.MediaAssets.Include(x => x.Variants).Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddAsync(MediaAsset asset, CancellationToken cancellationToken)
    {
        db.MediaAssets.Add(asset);
        return Task.CompletedTask;
    }

    public Task SaveAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
