namespace Ekiphan.Application.Media;

public sealed record AdminMediaTranslationInput(
    string LanguageCode,
    string Title,
    string? AltText,
    string? Description);

public sealed record SaveAdminMediaCommand(
    IReadOnlyList<AdminMediaTranslationInput> Translations,
    bool Archive);

public sealed record SetAdminMediaStatusCommand(bool Active);

public sealed record CreateExternalVideoCommand(
    string ExternalUrl,
    IReadOnlyList<AdminMediaTranslationInput> Translations);

public sealed record AdminMediaAssetDetail(
    Guid Id,
    string AssetType,
    string Status,
    string? OriginalFileName,
    string? MimeType,
    long? FileSizeBytes,
    string? Url,
    IReadOnlyList<AdminMediaTranslationInput> Translations,
    DateTimeOffset CreatedAt);

public sealed record AdminMediaAssignmentDetail(
    string TargetType,
    Guid TargetId,
    Guid MediaAssetId,
    string Role,
    bool IsDefault,
    int SortOrder);

public sealed record AdminMediaLibrary(
    IReadOnlyList<AdminMediaAssetDetail> Assets,
    IReadOnlyList<AdminMediaAssignmentDetail> Assignments,
    IReadOnlyList<AdminMediaTarget> Products,
    IReadOnlyList<AdminMediaTarget> Brands,
    IReadOnlyList<AdminMediaTarget> Categories,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record AdminMediaTarget(
    Guid Id,
    string Name,
    string? Code);

public sealed record SaveMediaAssignmentCommand(
    string TargetType,
    Guid TargetId,
    Guid MediaAssetId,
    string Role,
    bool IsDefault,
    int SortOrder);

public interface IAdminMediaService
{
    Task<AdminMediaLibrary> GetAsync(
        string? search,
        string? assetType,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<AdminMediaAssetDetail> CreateExternalVideoAsync(
        CreateExternalVideoCommand command,
        CancellationToken cancellationToken = default);
    Task<AdminMediaAssetDetail?> UpdateAssetAsync(
        Guid mediaAssetId,
        SaveAdminMediaCommand command,
        CancellationToken cancellationToken = default);
    Task<bool> DeletePdfAsync(
        Guid mediaAssetId,
        CancellationToken cancellationToken = default);
    Task<AdminMediaAssetDetail?> SetPdfStatusAsync(
        Guid mediaAssetId,
        SetAdminMediaStatusCommand command,
        CancellationToken cancellationToken = default);
    Task<AdminMediaAssignmentDetail> SaveAssignmentAsync(
        SaveMediaAssignmentCommand command,
        CancellationToken cancellationToken = default);
    Task<bool> RemoveAssignmentAsync(
        string targetType,
        Guid targetId,
        Guid mediaAssetId,
        string role,
        CancellationToken cancellationToken = default);
}

public sealed class AdminMediaConflictException(string message)
    : Exception(message);
