using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Media;

internal sealed class MediaAssetRepository(EkiphanDbContext dbContext)
    : IMediaAssetRepository
{
    public async Task AddAsync(
        MediaAsset asset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (await dbContext.MediaAssets.AnyAsync(
                item => item.Sha256Checksum == asset.Sha256Checksum,
                cancellationToken))
        {
            throw new DuplicateMediaContentException();
        }

        dbContext.MediaAssets.Add(asset);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(asset).State = EntityState.Detached;
            throw new DuplicateMediaContentException();
        }
    }
}
