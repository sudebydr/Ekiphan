using Ekiphan.Application.Media;

namespace Ekiphan.Infrastructure.Media;

internal sealed class UnavailableMediaThreatScanner : IMediaThreatScanner
{
    public Task<MediaThreatScanStatus> ScanAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MediaThreatScanStatus.Unavailable);
}
