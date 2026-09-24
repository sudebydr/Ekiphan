namespace Ekiphan.Domain.Deployments;

using System;
using Ekiphan.Domain.Common;

public enum DeploymentStatus
{
    Pending,
    AwaitingApproval,
    InProgress,
    Succeeded,
    Failed,
    RolledBack,
    PartiallyRolledBack,
    Cancelled
}

public sealed class DeploymentRecord : Entity
{
    public string Version { get; private set; } = default!;
    public string CommitSha { get; private set; } = default!;
    public string Environment { get; private set; } = default!;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DeploymentStatus Status { get; private set; }
    public string TriggeredBy { get; private set; } = default!;
    public string ImageTag { get; private set; } = default!;
    public string? ImageDigest { get; private set; }
    public string? PreviousVersion { get; private set; }
    public string? MigrationFrom { get; private set; }
    public string? MigrationTo { get; private set; }
    public string? MigrationScriptHash { get; private set; }
    public Guid? BackupId { get; private set; }
    public string? HealthCheckStatus { get; private set; }
    public string? SmokeTestStatus { get; private set; }
    public string? RollbackVersion { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    private DeploymentRecord() { }

    public DeploymentRecord(
        Guid id,
        string version,
        string commitSha,
        string environment,
        string triggeredBy,
        string imageTag,
        string? previousVersion) : base(id)
    {
        Version = version;
        CommitSha = commitSha;
        Environment = environment;
        TriggeredBy = triggeredBy;
        ImageTag = imageTag;
        PreviousVersion = previousVersion;
        Status = DeploymentStatus.Pending;
    }

    public void Start(DateTimeOffset startedAt)
    {
        StartedAt = startedAt;
        Status = DeploymentStatus.InProgress;
    }

    public void Complete(DateTimeOffset completedAt, string imageDigest)
    {
        CompletedAt = completedAt;
        ImageDigest = imageDigest;
        Status = DeploymentStatus.Succeeded;
    }

    public void Fail(DateTimeOffset failedAt, string errorCode, string errorMessage)
    {
        CompletedAt = failedAt;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Status = DeploymentStatus.Failed;
    }

    public void Rollback(string rollbackVersion)
    {
        RollbackVersion = rollbackVersion;
        Status = DeploymentStatus.RolledBack;
    }
}
