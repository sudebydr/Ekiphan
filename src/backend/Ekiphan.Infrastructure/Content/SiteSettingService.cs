using System.Text.Json;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Content;

public sealed class SiteSettingService(
    EkiphanDbContext dbContext,
    IUrlSafetyService urlSafetyService,
    ICmsRevisionService revisionService,
    ICmsCacheInvalidationService cacheService)
    : ISiteSettingService, IPublicSiteSettingService
{
    public async Task<IReadOnlyList<SiteSettingDto>> GetAllSettingsAsync(SiteSettingCategory? category = null, bool? isPublic = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<SiteSetting>().AsNoTracking();

        if (category.HasValue) query = query.Where(s => s.Category == category.Value);
        if (isPublic.HasValue) query = query.Where(s => s.IsPublic == isPublic.Value);

        var list = await query.ToListAsync(cancellationToken);

        return list.Select(s => new SiteSettingDto(
            s.Id,
            s.Key,
            s.Category,
            s.IsSensitive ? null : s.ValueJson,
            s.DataType,
            s.IsPublic,
            s.IsSensitive,
            !string.IsNullOrWhiteSpace(s.ValueJson),
            s.UpdatedAt,
            s.RowVersion)).ToList();
    }

    public async Task UpdateSettingsAsync(UpdateSiteSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var keys = command.Settings.Select(s => s.Key.ToLowerInvariant().Trim()).ToList();
        var existingSettings = await dbContext.Set<SiteSetting>()
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, cancellationToken);

        foreach (var item in command.Settings)
        {
            var key = item.Key.ToLowerInvariant().Trim();
            if (!existingSettings.TryGetValue(key, out var setting))
            {
                throw new KeyNotFoundException($"{CmsManagementErrorCodes.SettingKeyNotFound}: {item.Key}");
            }

            if (item.RowVersion.Length > 0 && setting.RowVersion.Length > 0 && !setting.RowVersion.SequenceEqual(item.RowVersion))
            {
                throw new DbUpdateConcurrencyException(CmsManagementErrorCodes.SettingConcurrencyConflict);
            }

            ValidateDataType(setting, item.Value);

            setting.UpdateValue(item.Value, command.ActorUserId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await revisionService.CreateRevisionAsync("SiteSetting", Guid.Empty, CmsRevisionChangeType.SettingsChanged, command.ActorUserId, command.Reason ?? "Updated site settings.", null, cancellationToken);
        await cacheService.InvalidateAsync(new CmsCacheInvalidationRequest(Settings: true), cancellationToken);
    }

    public async Task<PublicSiteSettingsDto> GetPublicSettingsAsync(CancellationToken cancellationToken = default)
    {
        var publicSettings = await dbContext.Set<SiteSetting>()
            .AsNoTracking()
            .Where(s => s.IsPublic && !s.IsSensitive)
            .ToListAsync(cancellationToken);

        var branding = publicSettings.Where(s => s.Category == SiteSettingCategory.Branding).ToDictionary(s => s.Key, s => ParseValue(s.ValueJson));
        var contact = publicSettings.Where(s => s.Category == SiteSettingCategory.Contact).ToDictionary(s => s.Key, s => ParseValue(s.ValueJson));
        var social = publicSettings.Where(s => s.Category == SiteSettingCategory.SocialMedia).ToDictionary(s => s.Key, s => ParseValue(s.ValueJson));
        var footer = publicSettings.Where(s => s.Category == SiteSettingCategory.Footer).ToDictionary(s => s.Key, s => ParseValue(s.ValueJson));
        var seo = publicSettings.Where(s => s.Category == SiteSettingCategory.Seo).ToDictionary(s => s.Key, s => ParseValue(s.ValueJson));
        var catalog = publicSettings.Where(s => s.Category == SiteSettingCategory.Catalog).ToDictionary(s => s.Key, s => ParseValue(s.ValueJson));
        var quote = publicSettings.Where(s => s.Category == SiteSettingCategory.Quote).ToDictionary(s => s.Key, s => ParseValue(s.ValueJson));
        var general = publicSettings.Where(s => s.Category == SiteSettingCategory.General).ToDictionary(s => s.Key, s => ParseValue(s.ValueJson));

        return new PublicSiteSettingsDto(branding, contact, social, footer, seo, catalog, quote, general);
    }

    private void ValidateDataType(SiteSetting setting, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        switch (setting.DataType)
        {
            case SiteSettingDataType.Boolean:
                if (!bool.TryParse(value, out _)) throw new ArgumentException(CmsManagementErrorCodes.SettingValueInvalid, setting.Key);
                break;
            case SiteSettingDataType.Integer:
                if (!int.TryParse(value, out _)) throw new ArgumentException(CmsManagementErrorCodes.SettingValueInvalid, setting.Key);
                break;
            case SiteSettingDataType.Decimal:
                if (!decimal.TryParse(value, out _)) throw new ArgumentException(CmsManagementErrorCodes.SettingValueInvalid, setting.Key);
                break;
            case SiteSettingDataType.Url:
                if (!urlSafetyService.IsSafeUrl(value)) throw new ArgumentException(CmsManagementErrorCodes.SettingValueInvalid, setting.Key);
                break;
            case SiteSettingDataType.Email:
                if (!value.Contains('@')) throw new ArgumentException(CmsManagementErrorCodes.SettingValueInvalid, setting.Key);
                break;
            case SiteSettingDataType.Json:
                try { using var doc = JsonDocument.Parse(value); }
                catch { throw new ArgumentException(CmsManagementErrorCodes.SettingValueInvalid, setting.Key); }
                break;
        }
    }

    private static object? ParseValue(string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(valueJson);
            return doc.RootElement.ValueKind switch
            {
                JsonValueKind.String => doc.RootElement.GetString(),
                JsonValueKind.Number => doc.RootElement.TryGetInt64(out var l) ? l : doc.RootElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => valueJson
            };
        }
        catch
        {
            return valueJson;
        }
    }
}
