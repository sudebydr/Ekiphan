using Ekiphan.Domain.Quotes;

namespace Ekiphan.Application.Quotes;

public sealed class QuoteEmailOptions
{
    public const string SectionName = "QuoteEmail";
    public int MaxRetryCount { get; set; } = 5;
    public List<int> RetryDelaysMinutes { get; set; } = [1, 5, 15, 60, 180];
    public bool SendCustomerConfirmation { get; set; } = true;
    public bool SendAdminNotification { get; set; } = true;
    public string AdminNotificationRecipient { get; set; } = "admin@ekiphan.com";
}

public sealed class BotProtectionOptions
{
    public const string SectionName = "BotProtection";
    public string Provider { get; set; } = "Turnstile";
    public bool Enabled { get; set; } = true;
    public double MinimumScore { get; set; } = 0.5;
    public bool FailClosed { get; set; } = true;
}

public sealed class QuoteRetentionOptions
{
    public const string SectionName = "QuoteRetention";
    public int RetentionDays { get; set; } = 730;
    public bool AnonymizeAfterRetention { get; set; } = true;
    public bool KeepOperationalStatistics { get; set; } = true;
}

public sealed record QuoteRequestContext(
    string? IpAddress,
    string? UserAgent,
    string CorrelationId,
    string? IdempotencyKey = null,
    string? BotToken = null);

public sealed record QuoteActivityDto(
    Guid Id,
    Guid QuoteRequestId,
    string ActivityType,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string? PreviousValue,
    string? NewValue,
    string Description,
    string? MetadataJson,
    string? IpAddress,
    DateTimeOffset CreatedAt);

public sealed record QuoteEmailStatusDto(
    string CustomerEmailStatus,
    string AdminEmailStatus,
    DateTimeOffset? LastAttemptAt,
    int AttemptCount,
    string? LastErrorCode,
    DateTimeOffset? SentAt);

public sealed record QuotePdfResult(
    byte[] PdfBytes,
    string FileName,
    string ContentType);

public sealed record BotVerificationResult(
    bool IsSuccess,
    double Score,
    string? ErrorCode,
    string? Message);

public sealed record QuoteSpamCheckResult(
    bool IsSpam,
    int RiskScore,
    IReadOnlyList<string> Reasons,
    SpamRecommendedAction RecommendedAction);

public sealed record UpdateQuoteNoteCommand(
    Guid QuoteId,
    Guid NoteId,
    string Text,
    byte[] ExpectedVersion,
    Guid AuthorUserId);

public sealed record DeleteQuoteNoteCommand(
    Guid QuoteId,
    Guid NoteId,
    byte[] ExpectedVersion,
    Guid DeletedByUserId);

public sealed record ArchiveQuoteCommand(
    Guid QuoteId,
    string Reason,
    byte[] ExpectedVersion,
    Guid ArchivedByUserId);

public sealed record PermanentDeleteQuoteCommand(
    Guid QuoteId,
    string Reason,
    string ReAuthToken,
    Guid DeletedByUserId);

public interface IQuoteStatusTransitionService
{
    bool CanTransition(QuoteStatus currentStatus, QuoteStatus targetStatus);
    IReadOnlyCollection<QuoteStatus> GetAllowedTransitions(QuoteStatus currentStatus);
}

public interface IQuoteActivityService
{
    Task LogAsync(
        Guid quoteId,
        QuoteActivityType activityType,
        Guid? actorUserId,
        string? previousValue,
        string? newValue,
        string description,
        object? metadata,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuoteActivityDto>> GetActivitiesAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);
}

public interface IQuoteExportService
{
    Task<byte[]> ExportCsvAsync(
        AdminQuoteListQuery query,
        bool includeSensitiveData,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportXlsxAsync(
        AdminQuoteListQuery query,
        bool includeSensitiveData,
        bool includeHistory,
        CancellationToken cancellationToken = default);
}

public interface IQuotePdfGenerator
{
    Task<QuotePdfResult> GenerateAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);
}

public interface IQuoteNotificationService
{
    Task QueueNewQuoteNotificationsAsync(
        Guid quoteId,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default);
}

public interface IQuoteEmailQueueService
{
    Task EnqueueAsync(
        EmailQueueItem message,
        CancellationToken cancellationToken = default);

    Task<QuoteEmailStatusDto> GetStatusAsync(
        Guid quoteId,
        CancellationToken cancellationToken = default);

    Task<bool> RetryFailedEmailAsync(
        Guid queueItemId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public interface IBotVerificationService
{
    Task<BotVerificationResult> VerifyAsync(
        string? token,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}

public interface IQuoteSpamDetectionService
{
    Task<QuoteSpamCheckResult> CheckAsync(
        SubmitQuoteCommand command,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default);
}

public interface IQuoteRetentionService
{
    Task ProcessRetentionPolicyAsync(CancellationToken cancellationToken = default);
}

public interface IQuoteDeletionService
{
    Task ArchiveAsync(
        ArchiveQuoteCommand command,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default);

    Task HardDeleteAsync(
        PermanentDeleteQuoteCommand command,
        QuoteRequestContext context,
        CancellationToken cancellationToken = default);
}
