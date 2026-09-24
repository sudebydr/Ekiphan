namespace Ekiphan.Domain.Media;

public enum MediaProcessingStatus
{
    Pending = 1,
    Scanning = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
    Rejected = 6,
    CompensationRequired = 7,
    Archived = 8,
}

public enum MediaVariantType
{
    Thumbnail = 1,
    Small = 2,
    Medium = 3,
    Large = 4,
}
