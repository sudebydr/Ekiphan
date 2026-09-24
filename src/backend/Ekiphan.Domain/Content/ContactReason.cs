using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class ContactReason : Entity
{
    private ContactReason() { }

    public ContactReason(
        Guid id,
        string name,
        int sortOrder,
        bool isComplaintReason = false) : base(id)
    {
        IsComplaintReason = isComplaintReason;
        Update(name, sortOrder);
    }

    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsComplaintReason { get; private set; }

    public void Update(string name, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = name.Trim();
        if (normalized.Length > 150 || sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(name));
        }

        Name = normalized;
        SortOrder = sortOrder;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
