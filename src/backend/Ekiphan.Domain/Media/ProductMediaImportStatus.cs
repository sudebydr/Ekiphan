namespace Ekiphan.Domain.Media;

public enum ProductMediaImportBatchStatus
{
    Uploaded = 1,
    Validating = 2,
    Validated = 3,
    Processing = 4,
    Completed = 5,
    PartiallyCompleted = 6,
    Failed = 7,
    CompensationRequired = 8,
    RolledBack = 9,
}

public enum ProductMediaImportBatchItemStatus
{
    Pending = 1,
    Matched = 2,
    Imported = 3,
    Skipped = 4,
    Duplicate = 5,
    Failed = 6,
    RolledBack = 7,
    RollbackSkipped = 8,
}
