namespace Ekiphan.Domain.Backups;

using System;
using Ekiphan.Domain.Common;

public enum BackupType
{
    Full,
    Differential,
    TransactionLog,
    PreDeployment,
    Manual
}

public enum BackupStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Verified,
    Expired,
    Deleted
}

public sealed class BackupRecord : Entity
{
    public BackupType BackupType { get; private set; }
    public string DatabaseName { get; private set; } = default!;
    public string Environment { get; private set; } = default!;
    public string StorageProvider { get; private set; } = default!;
    public string StorageKey { get; private set; } = default!;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public BackupStatus Status { get; private set; }
    public long? FileSize { get; private set; }
    public string? Checksum { get; private set; }
    public string? EncryptionKeyReference { get; private set; }
    public DateTimeOffset? RetentionUntil { get; private set; }
    public string TriggeredBy { get; private set; } = default!;
    public Guid? RelatedDeploymentId { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    private BackupRecord() { }


    public BackupRecord(
        Guid id,
        BackupType backupType,
        string databaseName,
        string environment,
        string storageProvider,
        string storageKey,
        string triggeredBy,
        Guid? relatedDeploymentId) : base(id)
    {
        BackupType = backupType;
        DatabaseName = databaseName;
        Environment = environment;
        StorageProvider = storageProvider;
        StorageKey = storageKey;
        TriggeredBy = triggeredBy;
        RelatedDeploymentId = relatedDeploymentId;
        Status = BackupStatus.Pending;
    }

    public void Start(DateTimeOffset startedAt)
    {
        StartedAt = startedAt;
        Status = BackupStatus.Running;
    }

    public void Complete(DateTimeOffset completedAt, long fileSize, string checksum, DateTimeOffset retentionUntil)
    {
        CompletedAt = completedAt;
        FileSize = fileSize;
        Checksum = checksum;
        RetentionUntil = retentionUntil;
        Status = BackupStatus.Completed;
    }

    public void Verify(DateTimeOffset verifiedAt)
    {
        VerifiedAt = verifiedAt;
        Status = BackupStatus.Verified;
    }

    public void Fail(DateTimeOffset failedAt, string errorCode, string errorMessage)
    {
        CompletedAt = failedAt;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Status = BackupStatus.Failed;
    }
}
