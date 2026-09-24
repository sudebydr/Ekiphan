using System.Security.Cryptography;
using System.Text;
using Ekiphan.Application.Content;
using Microsoft.Extensions.Caching.Memory;

namespace Ekiphan.Infrastructure.Content;

public sealed class CmsPreviewTokenService(IMemoryCache memoryCache) : ICmsPreviewTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    public Task<PreviewTokenResultDto> CreatePreviewTokenAsync(CreatePreviewTokenCommand command, CancellationToken cancellationToken = default)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "").Replace("/", "").Replace("=", "");

        var hashKey = GetCacheKey(rawToken);
        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

        var tokenData = new PreviewTokenData(command.EntityType.Trim(), command.EntityId, command.LanguageCode.Trim(), expiresAt);
        memoryCache.Set(hashKey, tokenData, TokenLifetime);

        var previewUrl = $"/api/preview/{command.EntityType}/{command.EntityId}?token={rawToken}&lang={command.LanguageCode}";
        return Task.FromResult(new PreviewTokenResultDto(rawToken, expiresAt, previewUrl, command.EntityType, command.EntityId, command.LanguageCode));
    }

    public Task<bool> ValidatePreviewTokenAsync(string token, string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return Task.FromResult(false);

        var hashKey = GetCacheKey(token.Trim());
        if (!memoryCache.TryGetValue<PreviewTokenData>(hashKey, out var data) || data is null)
            return Task.FromResult(false);

        if (data.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            memoryCache.Remove(hashKey);
            return Task.FromResult(false);
        }

        if (!data.EntityType.Equals(entityType.Trim(), StringComparison.OrdinalIgnoreCase) || data.EntityId != entityId)
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    private static string GetCacheKey(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return $"cms_preview_token_{Convert.ToHexString(hash)}";
    }

    private sealed record PreviewTokenData(string EntityType, Guid EntityId, string LanguageCode, DateTimeOffset ExpiresAt);
}
