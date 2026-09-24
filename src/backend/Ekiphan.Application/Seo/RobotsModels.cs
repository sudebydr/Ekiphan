namespace Ekiphan.Application.Seo;

public sealed record RobotsConfigurationDto(
    Guid Id,
    string EnvironmentName,
    string Content,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

public sealed record UpdateRobotsConfigurationCommand(
    string EnvironmentName,
    string Content,
    bool IsActive);

public sealed record RobotsPreviewResultDto(
    string EnvironmentName,
    string RenderedContent,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

public interface IRobotsConfigurationService
{
    Task<RobotsConfigurationDto?> GetByEnvironmentAsync(string environmentName, CancellationToken cancellationToken);
    Task<RobotsConfigurationDto> UpdateAsync(UpdateRobotsConfigurationCommand command, Guid actorUserId, CancellationToken cancellationToken);
    RobotsPreviewResultDto Preview(UpdateRobotsConfigurationCommand command);
    Task<RobotsConfigurationDto> ResetToDefaultAsync(string environmentName, Guid actorUserId, CancellationToken cancellationToken);
    Task<string> GetPublicRobotsTxtAsync(string environmentName, CancellationToken cancellationToken);
}
