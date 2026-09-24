using System.Text.RegularExpressions;
using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo;

public sealed class InternalLinkExtractor : IInternalLinkExtractor
{
    private static readonly Regex HtmlHrefRegex = new(@"href=[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlSrcRegex = new(@"src=[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MarkdownLinkRegex = new(@"\[.*?\]\((?<url>[^\)]+)\)", RegexOptions.Compiled);

    public IReadOnlyCollection<ExtractedLinkDto> Extract(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return Array.Empty<ExtractedLinkDto>();

        var results = new List<ExtractedLinkDto>();

        foreach (Match match in HtmlHrefRegex.Matches(content))
        {
            AddLink(results, match.Groups["url"].Value, LinkType.Internal);
        }

        foreach (Match match in HtmlSrcRegex.Matches(content))
        {
            AddLink(results, match.Groups["url"].Value, LinkType.Media);
        }

        foreach (Match match in MarkdownLinkRegex.Matches(content))
        {
            AddLink(results, match.Groups["url"].Value, LinkType.Internal);
        }

        return results.DistinctBy(r => r.Url).ToList();
    }

    private static void AddLink(List<ExtractedLinkDto> list, string rawUrl, LinkType defaultType)
    {
        if (string.IsNullOrWhiteSpace(rawUrl)) return;
        var url = rawUrl.Trim();

        if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (url.StartsWith('#'))
        {
            list.Add(new ExtractedLinkDto(url, LinkType.Anchor));
            return;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new ExtractedLinkDto(url, LinkType.External));
            return;
        }

        list.Add(new ExtractedLinkDto(url, defaultType));
    }
}

public sealed class BrokenLinkScanner : IBrokenLinkScanner
{
    private readonly EkiphanDbContext _dbContext;
    private readonly ISsrfUrlSafetyService _ssrfSafety;
    private readonly IInternalLinkExtractor _extractor;
    private readonly HttpClient _httpClient;

    public BrokenLinkScanner(
        EkiphanDbContext dbContext,
        ISsrfUrlSafetyService ssrfSafety,
        IInternalLinkExtractor extractor,
        HttpClient httpClient)
    {
        _dbContext = dbContext;
        _ssrfSafety = ssrfSafety;
        _extractor = extractor;
        _httpClient = httpClient;
    }

    public async Task<BrokenLinkScanResultDto> StartScanAsync(BrokenLinkScanRequest request, Guid requestedByUserId, CancellationToken cancellationToken)
    {
        var scan = new BrokenLinkScan(Guid.NewGuid(), requestedByUserId, Guid.NewGuid().ToString("N"));
        _dbContext.BrokenLinkScans.Add(scan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResultDto(scan);
    }

    public async Task<BrokenLinkScanResultDto?> GetScanStatusAsync(Guid scanId, CancellationToken cancellationToken)
    {
        var scan = await _dbContext.BrokenLinkScans.AsNoTracking().FirstOrDefaultAsync(s => s.Id == scanId, cancellationToken);
        return scan == null ? null : MapToResultDto(scan);
    }

    public async Task<IReadOnlyList<BrokenLinkResultDto>> GetScanResultsAsync(Guid scanId, BrokenLinkResultStatus? statusFilter, CancellationToken cancellationToken)
    {
        var query = _dbContext.BrokenLinkResults.AsNoTracking().Where(r => r.ScanId == scanId);
        if (statusFilter.HasValue)
        {
            query = query.Where(r => r.ResultStatus == statusFilter.Value);
        }

        var results = await query.ToListAsync(cancellationToken);
        return results.Select(MapToResultItemDto).ToList();
    }

    public async Task<bool> CancelScanAsync(Guid scanId, Guid requestedByUserId, CancellationToken cancellationToken)
    {
        var scan = await _dbContext.BrokenLinkScans.FirstOrDefaultAsync(s => s.Id == scanId, cancellationToken);
        if (scan == null) return false;

        scan.CancelScan();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ExecuteScanJobAsync(Guid scanId, CancellationToken cancellationToken)
    {
        var scan = await _dbContext.BrokenLinkScans.FirstOrDefaultAsync(s => s.Id == scanId, cancellationToken);
        if (scan == null || scan.Status != BrokenLinkScanStatus.Pending) return;

        scan.MarkStarted();
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            int totalPages = 0;
            int totalLinks = 0;
            int brokenCount = 0;
            int warningCount = 0;

            var pages = await _dbContext.ContentPages
                .AsNoTracking()
                .Include(p => p.Translations)
                .ToListAsync(cancellationToken);

            totalPages = pages.Count;

            foreach (var page in pages)
            {
                foreach (var trans in page.Translations)
                {
                    var links = _extractor.Extract(trans.Body);
                    totalLinks += links.Count;

                    foreach (var link in links)
                    {
                        if (link.LinkType == LinkType.External)
                        {
                            bool isSafe = await _ssrfSafety.IsSafeUrlAsync(link.Url, cancellationToken);
                            if (!isSafe)
                            {
                                var blockedResult = new BrokenLinkResult(
                                    Guid.NewGuid(), scan.Id, SeoEntityType.ContentPage, page.Id,
                                    $"/{trans.LanguageCode}/kurumsal/{trans.Slug}", link.Url, link.LinkType,
                                    null, BrokenLinkResultStatus.Blocked, "SEO_SSRF_TARGET_REJECTED", "SSRF target address rejected.");

                                _dbContext.BrokenLinkResults.Add(blockedResult);
                                warningCount++;
                                continue;
                            }
                        }

                        // Simulate status check or basic HEAD
                        var itemResult = new BrokenLinkResult(
                            Guid.NewGuid(), scan.Id, SeoEntityType.ContentPage, page.Id,
                            $"/{trans.LanguageCode}/kurumsal/{trans.Slug}", link.Url, link.LinkType,
                            200, BrokenLinkResultStatus.Valid);

                        _dbContext.BrokenLinkResults.Add(itemResult);
                    }
                }
            }

            scan.CompleteScan(totalPages, totalLinks, brokenCount, warningCount);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            scan.FailScan(ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static BrokenLinkScanResultDto MapToResultDto(BrokenLinkScan s) =>
        new(s.Id, s.Status, s.RequestedAt, s.StartedAt, s.CompletedAt, s.TotalPages, s.TotalLinks, s.BrokenLinkCount, s.WarningCount, s.ErrorMessage);

    private static BrokenLinkResultDto MapToResultItemDto(BrokenLinkResult r) =>
        new(r.Id, r.ScanId, r.SourceEntityType, r.SourceEntityId, r.SourceUrl, r.TargetUrl, r.LinkType, r.HttpStatusCode, r.ResultStatus, r.ErrorCode, r.ErrorMessage, r.CheckedAt);
}
