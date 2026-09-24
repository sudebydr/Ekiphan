namespace Ekiphan.Domain.DataImport;

public enum ImportJobStatus
{
    Uploaded = 1,
    Validating = 2,
    ReadyToPublish = 3,
    ValidationFailed = 4,
    Publishing = 5,
    Completed = 6,
    Failed = 7,
    Cancelled = 8,
}
