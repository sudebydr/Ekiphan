using Ekiphan.Domain.Content;

namespace Ekiphan.Application.Content;

public sealed record SubmitContactCommand(
    string FullName,
    string Email,
    string? Phone,
    string? CompanyName,
    string Subject,
    string Message,
    string LanguageCode,
    bool KvkkConsent,
    string? Website,
    Guid? ContactReasonId = null,
    Guid? ComplaintCategoryId = null);

public sealed record ContactSubmissionResult(Guid Id, DateTimeOffset ReceivedAt);

public enum AdminContactSortOrder
{
    Newest = 1,
    Oldest = 2,
}

public sealed record AdminContactListQuery(
    int Page = 1,
    int PageSize = 20,
    ContactRequestStatus? Status = null,
    string? Search = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    Guid? AssignedUserId = null,
    bool UnassignedOnly = false,
    bool NewOnly = false,
    AdminContactSortOrder SortOrder = AdminContactSortOrder.Newest);

public sealed record AdminContactPage(
    IReadOnlyList<AdminContactSummary> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record AdminContactSummary(
    Guid Id,
    DateTimeOffset CreatedAt,
    string FullName,
    string MaskedEmail,
    string? MaskedPhone,
    string Subject,
    string MessagePreview,
    ContactRequestStatus Status,
    Guid? AssignedToUserId,
    string? AssignedToDisplayName,
    DateTimeOffset UpdatedAt,
    string Version);

public sealed record AdminContactDetail(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? CompanyName,
    string Subject,
    string Message,
    string LanguageCode,
    DateTimeOffset ConsentAt,
    string ConsentVersion,
    ContactRequestStatus Status,
    Guid? AssignedToUserId,
    string? AssignedToDisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Version,
    string? ReasonName,
    string? ComplaintCategoryName,
    IReadOnlyList<AdminContactStatusHistoryItem> StatusHistory,
    IReadOnlyList<AdminContactNoteItem> InternalNotes);

public sealed record AdminContactStatusHistoryItem(
    Guid Id,
    ContactRequestStatus? FromStatus,
    ContactRequestStatus ToStatus,
    Guid? ChangedByUserId,
    string? ChangedByDisplayName,
    DateTimeOffset ChangedAt);

public sealed record AdminContactNoteItem(
    Guid Id,
    string Text,
    Guid AuthorUserId,
    string AuthorDisplayName,
    DateTimeOffset RecordedAt);

public sealed record AdminContactAssignee(Guid Id, string DisplayName);

public sealed record ChangeContactStatusCommand(
    Guid ContactRequestId,
    ContactRequestStatus Status,
    byte[] ExpectedVersion,
    Guid ChangedByUserId);

public sealed record AssignContactCommand(
    Guid ContactRequestId,
    Guid? AssignedToUserId,
    byte[] ExpectedVersion,
    Guid ChangedByUserId);

public sealed record AddContactNoteCommand(
    Guid ContactRequestId,
    string Text,
    byte[] ExpectedVersion,
    Guid AuthorUserId);

public sealed record AdminContactMutationResult(
    Guid Id,
    ContactRequestStatus Status,
    Guid? AssignedToUserId,
    DateTimeOffset UpdatedAt,
    string Version);

public sealed record AdminComplaintListQuery(
    int Page = 1,
    int PageSize = 20,
    ContactRequestStatus? Status = null,
    string? Search = null);

public sealed record AdminComplaintSummary(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? CompanyName,
    string CategoryName,
    string Subject,
    DateTimeOffset CreatedAt,
    ContactRequestStatus Status,
    Guid? AssignedToUserId,
    string? AssignedToDisplayName);

public sealed record AdminComplaintPage(
    IReadOnlyList<AdminComplaintSummary> Items,
    int Page,
    int PageSize,
    int TotalCount);

public interface IContactRequestService
{
    Task<ContactSubmissionResult> SubmitAsync(
        SubmitContactCommand command,
        string consentVersion,
        CancellationToken cancellationToken = default);

    Task<AdminContactPage> GetAdminAsync(
        AdminContactListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminContactDetail?> GetAdminDetailAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<AdminComplaintPage> GetComplaintsAsync(
        AdminComplaintListQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminContactAssignee>> GetAssigneesAsync(
        CancellationToken cancellationToken = default);

    Task<AdminContactMutationResult> ChangeStatusAsync(
        ChangeContactStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminContactMutationResult> AssignAsync(
        AssignContactCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminContactMutationResult> AddNoteAsync(
        AddContactNoteCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class ContactRequestNotFoundException(Guid requestId)
    : Exception($"Contact request '{requestId}' was not found.");

public sealed class ContactRequestConcurrencyException()
    : Exception("The contact request was changed by another user.");
