namespace Ekiphan.Application.Content;

public sealed record SaveContactReasonCommand(
    string Name,
    int SortOrder,
    bool IsActive,
    bool IsComplaintReason);

public sealed record SaveComplaintCategoryCommand(
    Guid ContactReasonId,
    string Name,
    int SortOrder,
    bool IsActive);

public sealed record AdminContactReason(
    Guid Id,
    string Name,
    int SortOrder,
    bool IsActive,
    bool IsComplaintReason);

public sealed record AdminComplaintCategory(
    Guid Id,
    Guid ContactReasonId,
    string Name,
    int SortOrder,
    bool IsActive);

public sealed record AdminContactTaxonomy(
    IReadOnlyList<AdminContactReason> Reasons,
    IReadOnlyList<AdminComplaintCategory> ComplaintCategories);

public sealed record PublicContactTaxonomy(
    IReadOnlyList<AdminContactReason> Reasons,
    IReadOnlyList<AdminComplaintCategory> ComplaintCategories);

public interface IContactTaxonomyService
{
    Task<AdminContactTaxonomy> GetAdminAsync(
        CancellationToken cancellationToken = default);

    Task<PublicContactTaxonomy> GetPublicAsync(
        CancellationToken cancellationToken = default);

    Task<AdminContactReason> CreateReasonAsync(
        SaveContactReasonCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminContactReason?> UpdateReasonAsync(
        Guid id,
        SaveContactReasonCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminComplaintCategory> CreateComplaintCategoryAsync(
        SaveComplaintCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminComplaintCategory?> UpdateComplaintCategoryAsync(
        Guid id,
        SaveComplaintCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteReasonAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteComplaintCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

public sealed class ContactTaxonomyConflictException(string message)
    : Exception(message);