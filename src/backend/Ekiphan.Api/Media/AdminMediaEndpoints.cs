using Ekiphan.Application.Media;
using Ekiphan.Domain.Media;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Media;

internal static class AdminMediaEndpoints
{
    public static void MapAdminMediaEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var library = endpoints.MapGroup("/api/admin/media-library");
        if (!authenticationConfigured)
        {
            endpoints.MapPost(
                    "/api/admin/media",
                    AuthenticationUnavailable)
                .WithMetadata(
                    new RequestSizeLimitAttribute(
                        MediaUploadLimits.MaximumRequestBytes))
                .RequireRateLimiting("media-upload");
            library.MapGet("", AuthenticationUnavailable);
            library.MapPost("/external-video", AuthenticationUnavailable);
            library.MapPut("/assets/{id:guid}", AuthenticationUnavailableForId);
            library.MapDelete("/assets/{id:guid}", AuthenticationUnavailableForId);
            library.MapPut("/assets/{id:guid}/status", AuthenticationUnavailableForId);
            library.MapPut("/assignments", AuthenticationUnavailable);
            library.MapDelete(
                "/assignments/{targetType}/{targetId:guid}/{mediaAssetId:guid}/{role}",
                AuthenticationUnavailableForAssignment);
            return;
        }

        endpoints.MapPost("/api/admin/media", UploadAsync)
            .WithMetadata(
                new RequestSizeLimitAttribute(
                    MediaUploadLimits.MaximumRequestBytes))
            .RequireAuthorization("MediaManage")
            .RequireRateLimiting("media-upload");
        library.RequireAuthorization("MediaManage");
        library.MapGet("", GetLibraryAsync)
            .RequireRateLimiting("catalog-admin-read");
        library.MapPost("/external-video", CreateExternalVideoAsync)
            .WithMetadata(new RequestSizeLimitAttribute(24 * 1024))
            .RequireRateLimiting("catalog-admin-write");
        library.MapPut("/assets/{id:guid}", UpdateAssetAsync)
            .WithMetadata(new RequestSizeLimitAttribute(24 * 1024))
            .RequireRateLimiting("catalog-admin-write");
        library.MapDelete("/assets/{id:guid}", DeletePdfAsync)
            .RequireRateLimiting("catalog-admin-write");
        library.MapPut("/assets/{id:guid}/status", SetPdfStatusAsync)
            .WithMetadata(new RequestSizeLimitAttribute(4 * 1024))
            .RequireRateLimiting("catalog-admin-write");
        library.MapPut("/assignments", SaveAssignmentAsync)
            .WithMetadata(new RequestSizeLimitAttribute(24 * 1024))
            .RequireRateLimiting("catalog-admin-write");
        library.MapDelete(
                "/assignments/{targetType}/{targetId:guid}/{mediaAssetId:guid}/{role}",
                RemoveAssignmentAsync)
            .RequireRateLimiting("catalog-admin-write");
    }

    private static async Task<IResult> GetLibraryAsync(
        HttpRequest request,
        HttpResponse response,
        IAdminMediaService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        var search = request.Query.TryGetValue("search", out var raw)
            ? raw.ToString().Trim()
            : null;
        if (search is { Length: > 100 })
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "search cannot exceed 100 characters.");
        }

        var assetType = request.Query.TryGetValue("assetType", out var rawType)
            ? rawType.ToString().Trim()
            : null;
        var status = request.Query.TryGetValue("status", out var rawStatus)
            ? rawStatus.ToString().Trim()
            : null;
        var page = request.Query.TryGetValue("page", out var rawPage) &&
            int.TryParse(rawPage, out var parsedPage) ? parsedPage : 1;
        var pageSize = request.Query.TryGetValue("pageSize", out var rawPageSize) &&
            int.TryParse(rawPageSize, out var parsedPageSize) ? parsedPageSize : 40;
        if (page < 1 || pageSize is < 1 or > 100)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid media paging.");
        }

        try
        {
            return Results.Ok(await service.GetAsync(
                search, assetType, status, page, pageSize, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static Task<IResult> CreateExternalVideoAsync(
        CreateExternalVideoRequest request,
        HttpResponse response,
        IAdminMediaService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.CreateExternalVideoAsync(
                new CreateExternalVideoCommand(
                    request.ExternalUrl,
                    Map(request.Translations)),
                cancellationToken);
            return Results.Created(
                $"/api/admin/media-library/assets/{value.Id}",
                value);
        });

    private static Task<IResult> UpdateAssetAsync(
        Guid id,
        SaveAssetRequest request,
        HttpResponse response,
        IAdminMediaService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.UpdateAssetAsync(
                id,
                new SaveAdminMediaCommand(
                    Map(request.Translations),
                    request.Archive),
                cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static Task<IResult> DeletePdfAsync(
        Guid id,
        HttpResponse response,
        IAdminMediaService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
            await service.DeletePdfAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound());

    private static Task<IResult> SetPdfStatusAsync(
        Guid id,
        SetAssetStatusRequest request,
        HttpResponse response,
        IAdminMediaService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () =>
        {
            var value = await service.SetPdfStatusAsync(
                id, new SetAdminMediaStatusCommand(request.Active), cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static Task<IResult> SaveAssignmentAsync(
        SaveAssignmentRequest request,
        HttpResponse response,
        IAdminMediaService service,
        CancellationToken cancellationToken) =>
        Execute(response, async () => Results.Ok(
            await service.SaveAssignmentAsync(
                new SaveMediaAssignmentCommand(
                    request.TargetType,
                    request.TargetId,
                    request.MediaAssetId,
                    request.Role,
                    request.IsDefault,
                    request.SortOrder),
                cancellationToken)));

    private static async Task<IResult> RemoveAssignmentAsync(
        string targetType,
        Guid targetId,
        Guid mediaAssetId,
        string role,
        HttpResponse response,
        IAdminMediaService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        try
        {
            return await service.RemoveAssignmentAsync(
                targetType,
                targetId,
                mediaAssetId,
                role,
                cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static async Task<IResult> Execute(
        HttpResponse response,
        Func<Task<IResult>> operation)
    {
        SetPrivateNoStore(response);
        try
        {
            return await operation();
        }
        catch (AdminMediaConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        HttpResponse response,
        MediaUploadService uploadService,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        if (!request.HasFormContentType)
        {
            return Problem(
                StatusCodes.Status415UnsupportedMediaType,
                "A multipart/form-data request is required.");
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return Problem(
                StatusCodes.Status413PayloadTooLarge,
                "The multipart body is malformed or exceeds the upload limit.");
        }

        if (form.Files.Count != 1 ||
            !TryGetAssetType(form, out var assetType) ||
            !TryGetRequiredText(form, "languageCode", 2, out var language) ||
            !TryGetRequiredText(form, "title", 250, out var title))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Exactly one file, a named assetType, languageCode and title are required.");
        }

        var file = form.Files[0];
        var maximumLength = assetType switch
        {
            MediaAssetType.Image => 4L * 1024 * 1024,
            MediaAssetType.Pdf => 500L * 1024 * 1024,
            _ => 50L * 1024 * 1024,
        };
        if (file.Length <= 0 || file.Length > maximumLength)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                $"File size must be between 1 byte and {maximumLength / 1024 / 1024} MB.");
        }

        var altText = form.TryGetValue("altText", out var rawAltText)
            ? rawAltText.ToString().Trim()
            : null;
        if (altText is { Length: > 500 })
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "altText cannot exceed 500 characters.");
        }

        var description = form.TryGetValue("description", out var rawDescription)
            ? rawDescription.ToString().Trim()
            : null;
        if (description is { Length: > 2000 })
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "description cannot exceed 2000 characters.");
        }

        try
        {
            await using var content = file.OpenReadStream();
            var result = await uploadService.UploadAsync(
                new UploadMediaCommand(
                    content,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    assetType,
                    language,
                    title,
                    altText,
                    description),
                cancellationToken);
            return Results.Created(
                $"/api/admin/media/{result.Id}",
                result);
        }
        catch (MediaUploadUnavailableException exception)
        {
            return Problem(
                StatusCodes.Status503ServiceUnavailable,
                exception.Message);
        }
        catch (DuplicateMediaContentException exception)
        {
            return Problem(
                StatusCodes.Status409Conflict,
                exception.Message);
        }
        catch (UnsafeMediaFileException exception)
        {
            return Problem(
                StatusCodes.Status422UnprocessableEntity,
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                exception.Message);
        }
    }

    private static bool TryGetAssetType(
        IFormCollection form,
        out MediaAssetType assetType)
    {
        assetType = default;
        if (!form.TryGetValue("assetType", out var rawValue))
        {
            return false;
        }

        var value = rawValue.ToString();
        return !int.TryParse(value, out _) &&
            Enum.TryParse(value, true, out assetType) &&
            Enum.IsDefined(assetType) &&
            assetType != MediaAssetType.ExternalVideo;
    }

    private static bool TryGetRequiredText(
        IFormCollection form,
        string key,
        int maximumLength,
        out string value)
    {
        value = form.TryGetValue(key, out var rawValue)
            ? rawValue.ToString().Trim()
            : string.Empty;
        return value.Length is > 0 && value.Length <= maximumLength;
    }

    private static void SetPrivateNoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult AuthenticationUnavailable() =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "Media administration is unavailable until JWT authentication is configured.");

    private static IResult AuthenticationUnavailableForId(Guid id) =>
        AuthenticationUnavailable();

    private static IResult AuthenticationUnavailableForAssignment(
        string targetType,
        Guid targetId,
        Guid mediaAssetId,
        string role) =>
        AuthenticationUnavailable();

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private static AdminMediaTranslationInput[] Map(
        IReadOnlyList<TranslationRequest>? translations) =>
        translations?.Select(item => new AdminMediaTranslationInput(
            item.LanguageCode,
            item.Title,
            item.AltText,
            item.Description)).ToArray() ?? [];

    private sealed record TranslationRequest(
        string LanguageCode,
        string Title,
        string? AltText,
        string? Description);

    private sealed record CreateExternalVideoRequest(
        string ExternalUrl,
        IReadOnlyList<TranslationRequest>? Translations);

    private sealed record SaveAssetRequest(
        bool Archive,
        IReadOnlyList<TranslationRequest>? Translations);

    private sealed record SetAssetStatusRequest(bool Active);

    private sealed record SaveAssignmentRequest(
        string TargetType,
        Guid TargetId,
        Guid MediaAssetId,
        string Role,
        bool IsDefault,
        int SortOrder);
}
