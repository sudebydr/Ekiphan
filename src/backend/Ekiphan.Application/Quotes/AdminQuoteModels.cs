using Ekiphan.Domain.Quotes;

namespace Ekiphan.Application.Quotes;

public enum AdminQuoteSortOrder
{
    Newest = 1,
    Oldest = 2,
}

public sealed record AdminQuoteListQuery(
    int Page = 1,
    int PageSize = 20,
    QuoteStatus? Status = null,
    string? Search = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    Guid? AssignedUserId = null,
    bool UnassignedOnly = false,
    string? ProductSearch = null,
    bool NewOnly = false,
    AdminQuoteSortOrder SortOrder = AdminQuoteSortOrder.Newest);

public sealed record AdminQuotePagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record AdminRequestAssignee(Guid Id, string DisplayName);

public sealed record AdminQuoteSummary(
    Guid Id,
    string RequestNumber,
    string FullName,
    string CompanyName,
    string MaskedEmail,
    string MaskedPhone,
    string Country,
    string? ProductName,
    string? SKU,
    QuoteStatus Status,
    Guid? AssignedToUserId,
    string? AssignedToDisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Version);

public sealed record AdminQuoteDetail(
    Guid Id,
    string RequestNumber,
    string FullName,
    string CompanyName,
    string Phone,
    string Email,
    string Country,
    string? City,
    string? Sector,
    string? ProjectName,
    string? Message,
    string LanguageCode,
    DateTimeOffset KvkkConsentAt,
    string KvkkConsentVersion,
    DateTimeOffset? CommercialCommunicationConsentAt,
    string? CommercialCommunicationConsentVersion,
    QuoteStatus Status,
    Guid? AssignedToUserId,
    string? AssignedToDisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Version,
    IReadOnlyList<AdminQuoteItem> Items,
    IReadOnlyList<AdminQuoteStatusHistoryItem> StatusHistory,
    IReadOnlyList<AdminQuoteInternalNoteItem> InternalNotes);

public sealed record AdminQuoteItem(
    Guid Id,
    Guid? ProductId,
    Guid? VariantId,
    string ProductName,
    string SKU,
    string? BrandName,
    int Quantity,
    string? VariantSnapshot,
    string? ProductNote,
    string? ImageStorageKey);

public sealed record AdminQuoteStatusHistoryItem(
    Guid Id,
    QuoteStatus? FromStatus,
    QuoteStatus ToStatus,
    DateTimeOffset ChangedAt,
    Guid? ChangedByUserId,
    string? ChangedByDisplayName,
    string? Note);

public sealed record AdminQuoteInternalNoteItem(
    Guid Id,
    string Text,
    DateTimeOffset RecordedAt,
    Guid AuthorUserId,
    string AuthorDisplayName);

public sealed record ChangeQuoteStatusCommand(
    Guid QuoteId,
    QuoteStatus Status,
    byte[] ExpectedVersion,
    Guid ChangedByUserId,
    string? Note);

public sealed record AssignQuoteCommand(
    Guid QuoteId,
    Guid? AssignedToUserId,
    byte[] ExpectedVersion,
    Guid ChangedByUserId);

public sealed record AddQuoteNoteCommand(
    Guid QuoteId,
    string Text,
    byte[] ExpectedVersion,
    Guid AuthorUserId);

public sealed record ChangeQuoteStatusResult(
    Guid Id,
    QuoteStatus Status,
    Guid? AssignedToUserId,
    DateTimeOffset UpdatedAt,
    string Version);
