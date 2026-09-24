using Ekiphan.Domain.Seo;

namespace Ekiphan.Application.Seo;

public sealed record RedirectRuleListItemDto(
    Guid Id,
    string SourcePath,
    string NormalizedSourcePath,
    string DestinationUrl,
    RedirectType RedirectType,
    RedirectMatchType MatchType,
    RedirectRuleSourceType SourceType,
    bool IsActive,
    bool PreserveQueryString,
    int Priority,
    long HitCount,
    DateTimeOffset? LastHitAt,
    DateTimeOffset CreatedAt);

public sealed record RedirectRuleDetailDto(
    Guid Id,
    string SourcePath,
    string NormalizedSourcePath,
    string DestinationUrl,
    RedirectType RedirectType,
    RedirectMatchType MatchType,
    RedirectRuleSourceType SourceType,
    SeoEntityType? SourceEntityType,
    Guid? SourceEntityId,
    bool IsActive,
    bool PreserveQueryString,
    int Priority,
    long HitCount,
    DateTimeOffset? LastHitAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record CreateRedirectRuleCommand(
    string SourcePath,
    string DestinationUrl,
    RedirectType RedirectType = RedirectType.Permanent301,
    RedirectMatchType MatchType = RedirectMatchType.Exact,
    bool PreserveQueryString = true,
    int Priority = 100);

public sealed record UpdateRedirectRuleCommand(
    string SourcePath,
    string DestinationUrl,
    RedirectType RedirectType,
    RedirectMatchType MatchType,
    bool PreserveQueryString,
    int Priority);

public sealed record ValidateRedirectRuleCommand(
    string SourcePath,
    string DestinationUrl,
    RedirectType RedirectType = RedirectType.Permanent301,
    RedirectMatchType MatchType = RedirectMatchType.Exact);

public sealed record RedirectValidationResultDto(
    bool IsValid,
    string NormalizedSourcePath,
    string NormalizedDestination,
    bool LoopDetected,
    int ChainLength,
    string FinalDestination,
    IReadOnlyList<string> Conflicts,
    IReadOnlyList<string> Warnings);

public sealed record SeoSlugChangeRequest(
    SeoEntityType EntityType,
    Guid EntityId,
    string LanguageCode,
    string OldSlug,
    string NewSlug);

public interface IRedirectRuleService
{
    Task<IReadOnlyList<RedirectRuleListItemDto>> GetListAsync(bool includeArchived, CancellationToken cancellationToken);
    Task<RedirectRuleDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<RedirectRuleDetailDto> CreateAsync(CreateRedirectRuleCommand command, Guid actorUserId, CancellationToken cancellationToken);
    Task<RedirectRuleDetailDto?> UpdateAsync(Guid id, UpdateRedirectRuleCommand command, Guid actorUserId, CancellationToken cancellationToken);
    Task<bool> ArchiveAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken);
    Task<bool> ActivateAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken);
    Task<RedirectValidationResultDto> ValidateAsync(ValidateRedirectRuleCommand command, CancellationToken cancellationToken);
}

public interface IRedirectResolver
{
    Task<RedirectRule?> ResolveAsync(string requestPath, string? queryString, CancellationToken cancellationToken);
}

public interface IRedirectLoopDetector
{
    Task<(bool HasLoop, int ChainLength, string FinalDestination)> DetectLoopAsync(string sourcePath, string destinationUrl, CancellationToken cancellationToken);
}

public interface ISeoRedirectCreationService
{
    Task CreateForSlugChangeAsync(SeoSlugChangeRequest request, CancellationToken cancellationToken);
}
