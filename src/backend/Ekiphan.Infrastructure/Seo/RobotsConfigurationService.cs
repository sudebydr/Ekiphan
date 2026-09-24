using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ekiphan.Infrastructure.Seo;

public sealed class RobotsConfigurationService : IRobotsConfigurationService
{
    private readonly EkiphanDbContext _dbContext;
    private readonly string _baseUrl;

    public RobotsConfigurationService(EkiphanDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _baseUrl = (configuration["SeoSettings:BaseUrl"] ?? "https://ekiphan.com").TrimEnd('/');
    }

    public async Task<RobotsConfigurationDto?> GetByEnvironmentAsync(string environmentName, CancellationToken cancellationToken)
    {
        var config = await _dbContext.RobotsConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EnvironmentName == environmentName && r.IsActive, cancellationToken);

        return config == null ? null : MapToDto(config);
    }

    public async Task<RobotsConfigurationDto> UpdateAsync(UpdateRobotsConfigurationCommand command, Guid actorUserId, CancellationToken cancellationToken)
    {
        var sanitizedContent = SanitizeContent(command.Content);

        var existing = await _dbContext.RobotsConfigurations
            .FirstOrDefaultAsync(r => r.EnvironmentName == command.EnvironmentName, cancellationToken);

        if (existing == null)
        {
            existing = new RobotsConfiguration(
                Guid.NewGuid(),
                command.EnvironmentName,
                sanitizedContent,
                command.IsActive,
                actorUserId);
            _dbContext.RobotsConfigurations.Add(existing);
        }
        else
        {
            existing.Update(sanitizedContent, command.IsActive, actorUserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(existing);
    }

    public RobotsPreviewResultDto Preview(UpdateRobotsConfigurationCommand command)
    {
        var sanitized = SanitizeContent(command.Content);
        var warnings = new List<string>();
        var errors = new List<string>();

        if (string.Equals(command.EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase))
        {
            if (sanitized.Contains("Disallow: /", StringComparison.OrdinalIgnoreCase) &&
                !sanitized.Contains("Disallow: /api", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("SEO_ROBOTS_PRODUCTION_BLOCK_WARNING: Production robots.txt blocks all search engine bots!");
            }
        }

        return new RobotsPreviewResultDto(command.EnvironmentName, sanitized, warnings, errors);
    }

    public async Task<RobotsConfigurationDto> ResetToDefaultAsync(string environmentName, Guid actorUserId, CancellationToken cancellationToken)
    {
        string defaultContent = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
            ? $"User-agent: *\nAllow: /\nSitemap: {_baseUrl}/sitemap-index.xml"
            : "User-agent: *\nDisallow: /";

        return await UpdateAsync(new UpdateRobotsConfigurationCommand(environmentName, defaultContent, true), actorUserId, cancellationToken);
    }

    public async Task<string> GetPublicRobotsTxtAsync(string environmentName, CancellationToken cancellationToken)
    {
        var config = await GetByEnvironmentAsync(environmentName, cancellationToken);
        if (config != null && !string.IsNullOrWhiteSpace(config.Content))
        {
            return config.Content;
        }

        return string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
            ? $"User-agent: *\nAllow: /\nSitemap: {_baseUrl}/sitemap-index.xml"
            : "User-agent: *\nDisallow: /";
    }

    private static string SanitizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        var cleaned = content.Replace("\0", string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");
        return cleaned.Trim();
    }

    private static RobotsConfigurationDto MapToDto(RobotsConfiguration entity) =>
        new(entity.Id, entity.EnvironmentName, entity.Content, entity.IsActive, entity.CreatedAt, entity.UpdatedAt, entity.PublishedAt);
}
