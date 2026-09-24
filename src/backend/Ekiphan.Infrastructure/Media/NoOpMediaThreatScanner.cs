using Ekiphan.Application.Media;

namespace Ekiphan.Infrastructure.Media;

// The project does not use a virus/threat scanning provider: media is
// uploaded from the trusted admin panel and stored directly, per team
// decision. This scanner always reports Clean instead of Unavailable so
// uploads are not blocked. Do not swap this for a real scanning service
// without also revisiting MediaUploadService's handling of ThreatFound.
internal sealed class NoOpMediaThreatScanner : IMediaThreatScanner
{
    public Task<MediaThreatScanStatus> ScanAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MediaThreatScanStatus.Clean);
}
