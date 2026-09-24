using Ekiphan.Application.DataImport;
using Ekiphan.Domain.DataImport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.DataImport;

internal static class ImportEndpoints
{
    private const long MaximumFileSizeBytes =
        ImportFileReadOptions.DefaultMaximumFileSizeBytes;

    public static IEndpointConventionBuilder MapImportUpload(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        if (authenticationConfigured)
        {
            return endpoints.MapPost("/api/admin/imports", UploadAsync)
                .WithMetadata(
                    new RequestSizeLimitAttribute(
                        MaximumFileSizeBytes + 64 * 1024))
                .RequireRateLimiting("import-upload")
                .RequireAuthorization("ImportManage");
        }

        return endpoints.MapPost(
                "/api/admin/imports",
                AuthenticationUnavailable)
            .RequireRateLimiting("import-upload");
    }

    public static void MapImportQueries(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        if (authenticationConfigured)
        {
            var group = endpoints.MapGroup("/api/admin/imports")
                .RequireAuthorization("ImportManage")
                .RequireRateLimiting("import-read");
            group.MapGet("", ListAsync);
            group.MapGet("/{jobId:guid}", GetAsync);
            group.MapGet("/{jobId:guid}/issues", ListIssuesAsync);
            group.MapGet("/{jobId:guid}/issues.csv", DownloadIssuesAsync);
            return;
        }

        endpoints.MapGet("/api/admin/imports", AuthenticationUnavailable)
            .RequireRateLimiting("import-read");
        endpoints.MapGet(
                "/api/admin/imports/{jobId:guid}",
                AuthenticationUnavailableForJob)
            .RequireRateLimiting("import-read");
        endpoints.MapGet(
                "/api/admin/imports/{jobId:guid}/issues",
                AuthenticationUnavailableForJob)
            .RequireRateLimiting("import-read");
        endpoints.MapGet(
                "/api/admin/imports/{jobId:guid}/issues.csv",
                AuthenticationUnavailableForJob)
            .RequireRateLimiting("import-read");
    }

    public static void MapImportPublishing(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        if (authenticationConfigured)
        {
            endpoints.MapPost(
                    "/api/admin/imports/{jobId:guid}/publish",
                    PublishAsync)
                .RequireAuthorization("ImportPublish")
                .RequireRateLimiting("import-upload");
            return;
        }

        endpoints.MapPost(
                "/api/admin/imports/{jobId:guid}/publish",
                AuthenticationUnavailableForJob)
            .RequireRateLimiting("import-upload");
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        ImportStagingService stagingService,
        CancellationToken cancellationToken)
    {
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

        if (form.Files.Count != 1)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Exactly one import file is required.");
        }

        var file = form.Files[0];
        if (file.Length == 0 || file.Length > MaximumFileSizeBytes)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                $"File size must be between 1 and {MaximumFileSizeBytes} bytes.");
        }

        if (!HasAllowedMediaType(file))
        {
            return Problem(
                StatusCodes.Status415UnsupportedMediaType,
                "The file extension and media type combination is not supported.");
        }

        var isDryRun = true;
        if (form.TryGetValue("isDryRun", out var dryRunValue))
        {
            if (!bool.TryParse(dryRunValue, out isDryRun))
            {
                return Problem(
                    StatusCodes.Status400BadRequest,
                    "isDryRun must be either true or false.");
            }
        }

        var userId = TryGetUserId(request.HttpContext.User);

        try
        {
            await using var content = file.OpenReadStream();
            var job = await stagingService.StageAsync(
                new StageImportFileCommand(
                    content,
                    file.FileName,
                    isDryRun,
                    userId),
                cancellationToken);
            var response = ToResponse(job);

            return job.Status == ImportJobStatus.Failed
                ? Results.UnprocessableEntity(response)
                : Results.Ok(response);
        }
        catch (DuplicateImportSourceException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
        catch (ImportFileReadException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static IResult AuthenticationUnavailable() =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "Import upload is unavailable until JWT authentication is configured.");

    private static IResult AuthenticationUnavailableForJob(Guid jobId) =>
        AuthenticationUnavailable();

    private static async Task<IResult> ListAsync(
        HttpRequest request,
        IImportQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!TryGetPagination(request, out var page, out var pageSize))
        {
            return InvalidPagination();
        }

        if (!TryGetEnumQuery(
                request,
                "status",
                out ImportJobStatus? status))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "status is not a valid import job status.");
        }

        return Results.Ok(
            await queryService.GetJobsAsync(
                page,
                pageSize,
                status,
                cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid jobId,
        IImportQueryService queryService,
        CancellationToken cancellationToken)
    {
        var job = await queryService.GetJobAsync(jobId, cancellationToken);
        return job is null ? Results.NotFound() : Results.Ok(job);
    }

    private static async Task<IResult> ListIssuesAsync(
        Guid jobId,
        HttpRequest request,
        IImportQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!TryGetPagination(request, out var page, out var pageSize))
        {
            return InvalidPagination();
        }

        if (!TryGetEnumQuery(
                request,
                "severity",
                out ImportIssueSeverity? severity))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "severity is not a valid import issue severity.");
        }

        var issues = await queryService.GetIssuesAsync(
            jobId,
            page,
            pageSize,
            severity,
            cancellationToken);
        return issues is null ? Results.NotFound() : Results.Ok(issues);
    }

    private static async Task<IResult> DownloadIssuesAsync(
        Guid jobId,
        HttpRequest request,
        IImportQueryService queryService,
        IImportIssueReportWriter reportWriter,
        CancellationToken cancellationToken)
    {
        if (!TryGetEnumQuery(
                request,
                "severity",
                out ImportIssueSeverity? severity))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "severity is not a valid import issue severity.");
        }

        if (await queryService.GetJobAsync(jobId, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        return Results.Stream(
            stream => reportWriter.WriteCsvAsync(
                jobId,
                stream,
                severity,
                cancellationToken),
            contentType: "text/csv; charset=utf-8",
            fileDownloadName: $"import-{jobId:N}-issues.csv");
    }

    private static async Task<IResult> PublishAsync(
        Guid jobId,
        ImportPublishingService publishingService,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(
                await publishingService.PublishAsync(
                    jobId,
                    cancellationToken));
        }
        catch (ImportJobNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Problem(StatusCodes.Status409Conflict, exception.Message);
        }
    }

    private static bool HasAllowedMediaType(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? file.ContentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase) ||
              file.ContentType.Equals(
                  "application/csv",
                  StringComparison.OrdinalIgnoreCase)
            : extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
              file.ContentType.Equals(
                  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                  StringComparison.OrdinalIgnoreCase);
    }

    private static Guid? TryGetUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        var value = user.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
            user.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var identifier) ? identifier : null;
    }

    private static ImportJobResponse ToResponse(ImportJob job) =>
        new(
            job.Id,
            job.OriginalFileName ?? string.Empty,
            job.SourceType,
            job.Status,
            job.IsDryRun,
            job.TotalRowCount,
            job.ValidRowCount,
            job.InvalidRowCount,
            job.WarningCount,
            job.FailureReason);

    private static IResult Problem(int statusCode, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail);

    private static bool TryGetPagination(
        HttpRequest request,
        out int page,
        out int pageSize)
    {
        page = 1;
        pageSize = 20;
        var pageValid = !request.Query.TryGetValue("page", out var pageValue) ||
            int.TryParse(pageValue, out page);
        var pageSizeValid =
            !request.Query.TryGetValue("pageSize", out var pageSizeValue) ||
            int.TryParse(pageSizeValue, out pageSize);
        return pageValid && pageSizeValid &&
            page >= 1 && pageSize is >= 1 and <= 100;
    }

    private static bool TryGetEnumQuery<TEnum>(
        HttpRequest request,
        string key,
        out TEnum? value)
        where TEnum : struct, Enum
    {
        value = null;
        if (!request.Query.TryGetValue(key, out var rawValue))
        {
            return true;
        }

        if (!Enum.TryParse<TEnum>(rawValue, ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private static IResult InvalidPagination() =>
        Problem(
            StatusCodes.Status400BadRequest,
            "page must be at least 1 and pageSize must be between 1 and 100.");
}
