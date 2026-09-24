using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo;

public sealed class RedirectService : IRedirectRuleService, IRedirectResolver, IRedirectLoopDetector, ISeoRedirectCreationService
{
    private readonly EkiphanDbContext _dbContext;
    private const int MaxChainLength = 5;

    public RedirectService(EkiphanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RedirectRuleListItemDto>> GetListAsync(bool includeArchived, CancellationToken cancellationToken)
    {
        var query = _dbContext.RedirectRules.AsNoTracking();
        if (!includeArchived)
        {
            query = query.Where(r => r.IsActive && r.ArchivedAt == null);
        }

        var rules = await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return rules.Select(MapToListItem).ToList();
    }

    public async Task<RedirectRuleDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.RedirectRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return rule == null ? null : MapToDetail(rule);
    }

    public async Task<RedirectRuleDetailDto> CreateAsync(CreateRedirectRuleCommand command, Guid actorUserId, CancellationToken cancellationToken)
    {
        var normalizedSource = NormalizePath(command.SourcePath);
        var validation = await ValidateAsync(new ValidateRedirectRuleCommand(command.SourcePath, command.DestinationUrl, command.RedirectType, command.MatchType), cancellationToken);

        if (!validation.IsValid || validation.LoopDetected)
        {
            string firstConflict = validation.Conflicts.Count > 0 ? validation.Conflicts[0] : "SEO_REDIRECT_LOOP_DETECTED: Loop detected.";
            throw new InvalidOperationException(firstConflict);
        }

        var rule = new RedirectRule(
            Guid.NewGuid(),
            command.SourcePath,
            normalizedSource,
            command.DestinationUrl,
            command.RedirectType,
            command.MatchType,
            RedirectRuleSourceType.Manual,
            command.PreserveQueryString,
            command.Priority,
            actorUserId);

        _dbContext.RedirectRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDetail(rule);
    }

    public async Task<RedirectRuleDetailDto?> UpdateAsync(Guid id, UpdateRedirectRuleCommand command, Guid actorUserId, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.RedirectRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule == null) return null;

        var normalizedSource = NormalizePath(command.SourcePath);
        var validation = await ValidateAsync(new ValidateRedirectRuleCommand(command.SourcePath, command.DestinationUrl, command.RedirectType, command.MatchType), cancellationToken);

        if (validation.LoopDetected)
        {
            throw new InvalidOperationException("SEO_REDIRECT_LOOP_DETECTED: Loop detected.");
        }

        rule.Update(
            command.SourcePath,
            normalizedSource,
            command.DestinationUrl,
            command.RedirectType,
            command.MatchType,
            command.PreserveQueryString,
            command.Priority,
            actorUserId);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDetail(rule);
    }

    public async Task<bool> ArchiveAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.RedirectRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule == null) return false;

        rule.Archive(actorUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ActivateAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.RedirectRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule == null) return false;

        rule.Activate(actorUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RedirectValidationResultDto> ValidateAsync(ValidateRedirectRuleCommand command, CancellationToken cancellationToken)
    {
        var normalizedSource = NormalizePath(command.SourcePath);
        var normalizedDest = command.DestinationUrl.Trim();

        var (hasLoop, chainLength, finalDest) = await DetectLoopAsync(normalizedSource, normalizedDest, cancellationToken);
        var conflicts = new List<string>();
        var warnings = new List<string>();

        if (hasLoop)
        {
            conflicts.Add("SEO_REDIRECT_LOOP_DETECTED: Redirect sequence causes a infinite loop.");
        }

        if (chainLength >= MaxChainLength)
        {
            warnings.Add($"SEO_REDIRECT_CHAIN_TOO_LONG: Redirect chain length ({chainLength}) reaches maximum boundary.");
        }

        var existingExact = await _dbContext.RedirectRules
            .AsNoTracking()
            .AnyAsync(r => r.NormalizedSourcePath == normalizedSource && r.IsActive && r.ArchivedAt == null, cancellationToken);

        if (existingExact)
        {
            conflicts.Add("SEO_REDIRECT_SOURCE_DUPLICATE: Active redirect rule already exists for this source path.");
        }

        bool isValid = conflicts.Count == 0;
        return new RedirectValidationResultDto(isValid, normalizedSource, normalizedDest, hasLoop, chainLength, finalDest, conflicts, warnings);
    }

    public async Task<RedirectRule?> ResolveAsync(string requestPath, string? queryString, CancellationToken cancellationToken)
    {
        var normalized = NormalizePath(requestPath);

        var exactRule = await _dbContext.RedirectRules
            .AsNoTracking()
            .Where(r => r.IsActive && r.ArchivedAt == null && r.MatchType == RedirectMatchType.Exact && r.NormalizedSourcePath == normalized)
            .OrderByDescending(r => r.Priority)
            .FirstOrDefaultAsync(cancellationToken);

        if (exactRule != null) return exactRule;

        var prefixRules = await _dbContext.RedirectRules
            .AsNoTracking()
            .Where(r => r.IsActive && r.ArchivedAt == null && r.MatchType == RedirectMatchType.Prefix)
            .OrderByDescending(r => r.NormalizedSourcePath.Length)
            .ThenByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);

        return prefixRules.FirstOrDefault(r => normalized.StartsWith(r.NormalizedSourcePath, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(bool HasLoop, int ChainLength, string FinalDestination)> DetectLoopAsync(string sourcePath, string destinationUrl, CancellationToken cancellationToken)
    {
        var currentSource = NormalizePath(sourcePath);
        var currentDest = destinationUrl.Trim();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { currentSource };
        int length = 1;

        while (length < MaxChainLength + 2)
        {
            if (currentDest.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || currentDest.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return (false, length, currentDest);
            }

            var normalizedDestPath = NormalizePath(currentDest);
            if (visited.Contains(normalizedDestPath))
            {
                return (true, length, currentDest);
            }

            visited.Add(normalizedDestPath);

            var nextRule = await _dbContext.RedirectRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IsActive && r.ArchivedAt == null && r.NormalizedSourcePath == normalizedDestPath, cancellationToken);

            if (nextRule == null) break;

            currentDest = nextRule.DestinationUrl;
            length++;
        }

        return (false, length, currentDest);
    }

    public async Task CreateForSlugChangeAsync(SeoSlugChangeRequest request, CancellationToken cancellationToken)
    {
        if (string.Equals(request.OldSlug, request.NewSlug, StringComparison.OrdinalIgnoreCase)) return;

        string sectionPath = request.EntityType switch
        {
            SeoEntityType.Product => "urunler",
            SeoEntityType.Category => "kategoriler",
            SeoEntityType.Brand => "markalar",
            SeoEntityType.NewsArticle => "basin",
            SeoEntityType.ReferenceProject => "referanslar",
            SeoEntityType.Showroom => "showroom",
            _ => "kurumsal"
        };

        string sourcePath = $"/{request.LanguageCode.ToLowerInvariant()}/{sectionPath}/{request.OldSlug}";
        string destUrl = $"/{request.LanguageCode.ToLowerInvariant()}/{sectionPath}/{request.NewSlug}";
        string normalizedSource = NormalizePath(sourcePath);

        var existing = await _dbContext.RedirectRules
            .FirstOrDefaultAsync(r => r.NormalizedSourcePath == normalizedSource && r.IsActive, cancellationToken);

        if (existing != null) return;

        var rule = new RedirectRule(
            Guid.NewGuid(),
            sourcePath,
            normalizedSource,
            destUrl,
            RedirectType.Permanent301,
            RedirectMatchType.Exact,
            RedirectRuleSourceType.SlugChange,
            true,
            100,
            Guid.Empty,
            request.EntityType,
            request.EntityId);

        _dbContext.RedirectRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var p = path.Trim().ToLowerInvariant().Split('?')[0];
        return p.StartsWith('/') ? p.TrimEnd('/') : $"/{p.TrimEnd('/')}";
    }

    private static RedirectRuleListItemDto MapToListItem(RedirectRule r) =>
        new(r.Id, r.SourcePath, r.NormalizedSourcePath, r.DestinationUrl, r.RedirectType, r.MatchType, r.SourceType, r.IsActive, r.PreserveQueryString, r.Priority, r.HitCount, r.LastHitAt, r.CreatedAt);

    private static RedirectRuleDetailDto MapToDetail(RedirectRule r) =>
        new(r.Id, r.SourcePath, r.NormalizedSourcePath, r.DestinationUrl, r.RedirectType, r.MatchType, r.SourceType, r.SourceEntityType, r.SourceEntityId, r.IsActive, r.PreserveQueryString, r.Priority, r.HitCount, r.LastHitAt, r.CreatedAt, r.UpdatedAt, r.ArchivedAt);
}
