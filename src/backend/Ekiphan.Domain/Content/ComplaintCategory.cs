using Ekiphan.Domain.Common;

namespace Ekiphan.Domain.Content;

public sealed class ComplaintCategory : Entity
{
    private ComplaintCategory() { }

    public ComplaintCategory(
        Guid id,
        Guid contactReasonId,
        string name,
        int sortOrder) : base(id)
    {
        if (contactReasonId == Guid.Empty)
        {
            throw new ArgumentException("Contact reason is required.", nameof(contactReasonId));
        }

        ContactReasonId = contactReasonId;
        Update(name, sortOrder);
    }

    public Guid ContactReasonId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

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
