using Ekiphan.Domain.Seo;

namespace Ekiphan.Application.Seo;

public sealed record BrokenLinkScanRequest(
    bool CheckExternalLinks = true,
    int? MaxPages = 20000);

public sealed record BrokenLinkScanResultDto(
    Guid ScanId,
    BrokenLinkScanStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int TotalPages,
    int TotalLinks,
    int BrokenLinkCount,
    int WarningCount,
    string? ErrorMessage);

public sealed record BrokenLinkResultDto(
    Guid Id,
    Guid ScanId,
    SeoEntityType SourceEntityType,
    Guid SourceEntityId,
    string SourceUrl,
    string TargetUrl,
    LinkType LinkType,
    int? HttpStatusCode,
    BrokenLinkResultStatus ResultStatus,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CheckedAt);

public sealed record ExtractedLinkDto(
    string Url,
    LinkType LinkType);

public interface IBrokenLinkScanner
{
    Task<BrokenLinkScanResultDto> StartScanAsync(BrokenLinkScanRequest request, Guid requestedByUserId, CancellationToken cancellationToken);
    Task<BrokenLinkScanResultDto?> GetScanStatusAsync(Guid scanId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BrokenLinkResultDto>> GetScanResultsAsync(Guid scanId, BrokenLinkResultStatus? statusFilter, CancellationToken cancellationToken);
    Task<bool> CancelScanAsync(Guid scanId, Guid requestedByUserId, CancellationToken cancellationToken);
    Task ExecuteScanJobAsync(Guid scanId, CancellationToken cancellationToken);
}

public interface IInternalLinkExtractor
{
    IReadOnlyCollection<ExtractedLinkDto> Extract(string content);
}

public interface ISsrfUrlSafetyService
{
    Task<bool> IsSafeUrlAsync(string url, CancellationToken cancellationToken);
}
