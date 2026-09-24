using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Seo;

public sealed class BrokenLinkResult : Entity
{
    private BrokenLinkResult() { }

    public BrokenLinkResult(
        Guid id,
        Guid scanId,
        SeoEntityType sourceEntityType,
        Guid sourceEntityId,
        string sourceUrl,
        string targetUrl,
        LinkType linkType,
        int? httpStatusCode,
        BrokenLinkResultStatus resultStatus,
        string? errorCode = null,
        string? errorMessage = null)
        : base(id)
    {
        ScanId = scanId;
        SourceEntityType = sourceEntityType;
        SourceEntityId = sourceEntityId;
        SourceUrl = sourceUrl ?? string.Empty;
        TargetUrl = targetUrl ?? string.Empty;
        LinkType = linkType;
        HttpStatusCode = httpStatusCode;
        ResultStatus = resultStatus;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CheckedAt = DateTimeOffset.UtcNow;
    }

    public Guid ScanId { get; private set; }
    public BrokenLinkScan? Scan { get; private set; }
    public SeoEntityType SourceEntityType { get; private set; }
    public Guid SourceEntityId { get; private set; }
    public string SourceUrl { get; private set; } = string.Empty;
    public string TargetUrl { get; private set; } = string.Empty;
    public LinkType LinkType { get; private set; }
    public int? HttpStatusCode { get; private set; }
    public BrokenLinkResultStatus ResultStatus { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CheckedAt { get; private set; }
}
