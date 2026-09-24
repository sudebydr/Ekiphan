using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Seo;

public sealed class RobotsConfiguration : Entity
{
    private RobotsConfiguration() { }

    public RobotsConfiguration(
        Guid id,
        string environmentName,
        string content,
        bool isActive,
        Guid createdByUserId)
        : base(id)
    {
        EnvironmentName = environmentName ?? throw new ArgumentNullException(nameof(environmentName));
        Content = content ?? string.Empty;
        IsActive = isActive;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
        RowVersion = Array.Empty<byte>();
    }

    public string EnvironmentName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public void Update(string content, bool isActive, Guid actorUserId)
    {
        Content = content ?? string.Empty;
        IsActive = isActive;
        UpdatedByUserId = actorUserId;
        if (isActive)
        {
            PublishedAt = DateTimeOffset.UtcNow;
        }
    }
}
