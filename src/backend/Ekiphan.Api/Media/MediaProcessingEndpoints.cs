using System.Security.Claims;
using Ekiphan.Application.Media;
using Microsoft.AspNetCore.Mvc;

namespace Ekiphan.Api.Media;

internal static class MediaProcessingEndpoints
{
    public static void MapMediaProcessingEndpoints(this IEndpointRouteBuilder endpoints, bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/media");
        if (!authenticationConfigured)
        {
            group.MapPost("/upload", () => Results.Problem("Authentication is not configured.", statusCode: 503));
            group.MapGet("/{id:guid}/processing-status", () => Results.Problem("Authentication is not configured.", statusCode: 503));
            return;
        }

        group.MapPost("/upload", UploadAsync)
            .WithSummary("Upload and optimize an image")
            .WithDescription("Validates an image, strips sensitive metadata and creates WebP variants.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<MediaUploadResultDto>(201).ProducesProblem(400).ProducesProblem(413)
            .ProducesProblem(422).ProducesProblem(503)
            .RequireAuthorization("MediaUpload").RequireRateLimiting("media-upload")
            .WithMetadata(new RequestSizeLimitAttribute(MediaUploadLimits.MaximumRequestBytes));
        group.MapGet("/{id:guid}/processing-status", StatusAsync)
            .WithSummary("Get media processing status").Produces<MediaProcessingStatusDto>()
            .ProducesProblem(404).RequireAuthorization("MediaRead");
        group.MapPost("/{id:guid}/retry-processing", RetryAsync)
            .WithSummary("Retry failed media processing").Produces<MediaProcessingStatusDto>()
            .ProducesProblem(404).ProducesProblem(409).RequireAuthorization("MediaRetry");
        group.MapPost("/{id:guid}/archive", ArchiveAsync)
            .WithSummary("Soft archive a media asset").Produces(204).ProducesProblem(404)
            .RequireAuthorization("MediaArchive");
    }

    private static async Task<IResult> UploadAsync(HttpRequest request, ClaimsPrincipal user,
        IMediaProcessingService service, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType) return Problem(415, "MEDIA_MIME_TYPE_INVALID", "multipart/form-data is required.");
        IFormCollection form;
        try { form = await request.ReadFormAsync(cancellationToken); }
        catch (InvalidDataException) { return Problem(413, MediaProcessingErrorCodes.FileTooLarge, "Upload exceeds the configured request limit."); }
        if (form.Files.Count != 1) return Problem(400, MediaProcessingErrorCodes.FileRequired, "Exactly one image file is required.");
        var file = form.Files[0];
        var mode = Enum.TryParse<MediaProcessingMode>(form["processingMode"], true, out var parsed)
            ? parsed : MediaProcessingMode.Auto;
        Guid? userId = Guid.TryParse(user.FindFirstValue("sub"), out var parsedUserId) ? parsedUserId : null;
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await service.ProcessAsync(new MediaUploadCommand(stream, file.FileName,
                file.ContentType, file.Length, Value(form, "languageCode", "tr")!, Value(form, "title", file.FileName)!,
                Value(form, "altText", Path.GetFileNameWithoutExtension(file.FileName))!,
                Value(form, "description", null), mode, userId), cancellationToken);
            return Results.Created($"/api/admin/media/{result.MediaAssetId}/processing-status", result);
        }
        catch (MediaProcessingException exception) { return Problem(exception.StatusCode, exception.Code, exception.Message); }
    }

    private static async Task<IResult> StatusAsync(Guid id, IMediaProcessingService service, CancellationToken cancellationToken)
    {
        var result = await service.GetStatusAsync(id, cancellationToken);
        return result is null ? Problem(404, MediaProcessingErrorCodes.NotFound, "Media asset was not found.") : Results.Ok(result);
    }

    private static async Task<IResult> RetryAsync(Guid id, IMediaProcessingService service, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await service.RetryAsync(id, cancellationToken)); }
        catch (MediaProcessingException exception) { return Problem(exception.StatusCode, exception.Code, exception.Message); }
    }

    private static async Task<IResult> ArchiveAsync(Guid id, IMediaProcessingService service, CancellationToken cancellationToken) =>
        await service.ArchiveAsync(id, cancellationToken) ? Results.NoContent() :
            Problem(404, MediaProcessingErrorCodes.NotFound, "Media asset was not found.");

    private static string? Value(IFormCollection form, string key, string? fallback) =>
        form.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.ToString().Trim() : fallback;

    private static IResult Problem(int status, string code, string detail) => Results.Problem(
        statusCode: status, title: code, detail: detail, extensions: new Dictionary<string, object?> { ["code"] = code });
}
