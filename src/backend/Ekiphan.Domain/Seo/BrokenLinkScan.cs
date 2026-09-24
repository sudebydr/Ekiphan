using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Seo;

public sealed class BrokenLinkScan : Entity
{
    private BrokenLinkScan() { }

    public BrokenLinkScan(Guid id, Guid requestedByUserId, string? correlationId = null)
        : base(id)
    {
        RequestedByUserId = requestedByUserId;
        RequestedAt = DateTimeOffset.UtcNow;
        Status = BrokenLinkScanStatus.Pending;
        CorrelationId = correlationId;
    }

    public Guid RequestedByUserId { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public BrokenLinkScanStatus Status { get; private set; } = BrokenLinkScanStatus.Pending;
    public int TotalPages { get; private set; }
    public int TotalLinks { get; private set; }
    public int BrokenLinkCount { get; private set; }
    public int WarningCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? CorrelationId { get; private set; }

    public ICollection<BrokenLinkResult> Results { get; private set; } = new List<BrokenLinkResult>();

    public void MarkStarted()
    {
        StartedAt = DateTimeOffset.UtcNow;
        Status = BrokenLinkScanStatus.Processing;
    }

    public void CompleteScan(int totalPages, int totalLinks, int brokenCount, int warningCount)
    {
        CompletedAt = DateTimeOffset.UtcNow;
        Status = BrokenLinkScanStatus.Completed;
        TotalPages = totalPages;
        TotalLinks = totalLinks;
        BrokenLinkCount = brokenCount;
        WarningCount = warningCount;
    }

    public void FailScan(string errorMessage)
    {
        CompletedAt = DateTimeOffset.UtcNow;
        Status = BrokenLinkScanStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void CancelScan()
    {
        CompletedAt = DateTimeOffset.UtcNow;
        Status = BrokenLinkScanStatus.Cancelled;
    }
}
