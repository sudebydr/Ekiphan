namespace Ekiphan.Domain.DataImport;

public enum ImportBatchStatus
{
    Uploaded = 1,
    Validating = 2,
    Validated = 3,
    Processing = 4,
    Completed = 5,
    PartiallyCompleted = 6,
    Failed = 7,
    RolledBack = 8,
}

public enum ImportBatchItemStatus
{
    Valid = 1,
    Invalid = 2,
    Imported = 3,
    RollbackSkipped = 4,
    RolledBack = 5,
}
