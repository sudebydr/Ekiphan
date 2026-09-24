using System.Globalization;
using System.Security.Claims;
using Ekiphan.Application.Quotes;
using Ekiphan.Domain.Identity;
using Ekiphan.Domain.Quotes;
using Microsoft.AspNetCore.Mvc;

namespace Ekiphan.Api.Quotes;

internal static class AdminQuoteEndpoints
{
    public static void MapAdminQuoteEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/quotes")
            .RequireRateLimiting("quote-admin-read");

        if (!authenticationConfigured)
        {
            group.MapGet("", Unavailable);
            group.MapGet("/assignees", Unavailable);
            group.MapGet("/{quoteId:guid}", UnavailableForQuote);
            group.MapPost("/{quoteId:guid}/status", UnavailableForQuote);
            group.MapPost("/{quoteId:guid}/assignment", UnavailableForQuote);
            group.MapPost("/{quoteId:guid}/notes", UnavailableForQuote);
            return;
        }

        group.RequireAuthorization("QuoteRead");

        group.MapGet("", ListAsync)
            .WithSummary("List quotes")
            .WithDescription("Retrieves paged quote requests with filtering and permission-based masking.");

        group.MapGet("/assignees", GetAssigneesAsync)
            .WithSummary("List quote assignees");

        group.MapGet("/{quoteId:guid}", GetAsync)
            .WithSummary("Get quote details");

        group.MapPost("/{quoteId:guid}/assign", AssignAsync)
            .RequireAuthorization("QuoteManage")
            .WithSummary("Assign quote to operator");

        group.MapDelete("/{quoteId:guid}/assignment", UnassignAsync)
            .RequireAuthorization("QuoteManage")
            .WithSummary("Unassign quote operator");

        group.MapPost("/{quoteId:guid}/status", ChangeStatusAsync)
            .RequireAuthorization("QuoteManage")
            .WithSummary("Change quote status");

        group.MapPost("/{quoteId:guid}/notes", AddNoteAsync)
            .RequireAuthorization("QuoteManage")
            .WithSummary("Add internal note");

        group.MapPut("/{quoteId:guid}/notes/{noteId:guid}", UpdateNoteAsync)
            .RequireAuthorization("QuoteManage")
            .WithSummary("Update internal note");

        group.MapDelete("/{quoteId:guid}/notes/{noteId:guid}", DeleteNoteAsync)
            .RequireAuthorization("QuoteManage")
            .WithSummary("Soft delete internal note");

        group.MapGet("/{quoteId:guid}/activities", GetActivitiesAsync)
            .RequireAuthorization("QuoteRead")
            .WithSummary("Get quote activity history");

        group.MapGet("/{quoteId:guid}/email-status", GetEmailStatusAsync)
            .RequireAuthorization("QuoteRead")
            .WithSummary("Get email delivery status");

        group.MapPost("/{quoteId:guid}/email/retry", RetryEmailAsync)
            .RequireAuthorization("QuoteManage")
            .WithSummary("Retry failed email queue item");

        group.MapGet("/export/csv", ExportCsvAsync)
            .RequireAuthorization("QuoteRead")
            .WithSummary("Export quotes as CSV");

        group.MapGet("/export/xlsx", ExportXlsxAsync)
            .RequireAuthorization("QuoteRead")
            .WithSummary("Export quotes as XLSX");

        group.MapGet("/{quoteId:guid}/pdf", GetPdfAsync)
            .RequireAuthorization("QuoteRead")
            .WithSummary("Generate quote PDF document");

        group.MapPost("/{quoteId:guid}/archive", ArchiveAsync)
            .RequireAuthorization("QuoteManage")
            .WithSummary("Archive quote request");

        group.MapDelete("/{quoteId:guid}", PermanentDeleteAsync)
            .RequireAuthorization("QuoteManage")
            .WithSummary("Permanently delete quote request (Requires Re-Auth)");
    }

    private static async Task<IResult> ListAsync(
        HttpContext context,
        IAdminQuoteQueryService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(context.Response);
        if (!TryBuildQuery(context.Request, out var query, out var detail))
        {
            return Problem(400, detail);
        }

        try
        {
            return Results.Ok(await service.GetQuotesAsync(query!, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Problem(400, exception.Message);
        }
    }

    private static async Task<IResult> GetAsync(
        Guid quoteId,
        HttpContext context,
        IAdminQuoteQueryService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(context.Response);
        var quote = await service.GetQuoteAsync(quoteId, cancellationToken);
        return quote is null ? Results.NotFound() : Results.Ok(quote);
    }

    private static async Task<IResult> GetAssigneesAsync(
        HttpResponse response,
        IAdminQuoteQueryService service,
        CancellationToken cancellationToken)
    {
        SetPrivateNoStore(response);
        return Results.Ok(await service.GetAssigneesAsync(cancellationToken));
    }

    private static async Task<IResult> AssignAsync(
        Guid quoteId,
        [FromBody] AssignQuoteRequest request,
        HttpContext context,
        QuoteManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryMutation(context, request.ExpectedVersion, out var actor, out var version, out var problem))
        {
            return problem!;
        }

        try
        {
            return Results.Ok(await service.AssignAsync(
                new AssignQuoteCommand(quoteId, request.AssignedToUserId, version!, actor),
                Context(context),
                cancellationToken));
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> UnassignAsync(
        Guid quoteId,
        [FromBody] UnassignQuoteRequest request,
        HttpContext context,
        QuoteManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryMutation(context, request.ExpectedVersion, out var actor, out var version, out var problem))
        {
            return problem!;
        }

        try
        {
            return Results.Ok(await service.AssignAsync(
                new AssignQuoteCommand(quoteId, null, version!, actor),
                Context(context),
                cancellationToken));
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid quoteId,
        [FromBody] ChangeQuoteStatusRequest request,
        HttpContext context,
        QuoteManagementService service,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return Problem(400, "Status is invalid.");
        }
        if (!TryMutation(context, request.ExpectedVersion, out var actor, out var version, out var problem))
        {
            return problem!;
        }

        try
        {
            return Results.Ok(await service.ChangeStatusAsync(
                new ChangeQuoteStatusCommand(quoteId, request.Status, version!, actor, request.Note),
                Context(context),
                cancellationToken));
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> AddNoteAsync(
        Guid quoteId,
        [FromBody] AddQuoteNoteRequest request,
        HttpContext context,
        QuoteManagementService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 2000)
        {
            return Problem(400, "Note must contain between 1 and 2000 characters.");
        }
        if (!TryMutation(context, request.ExpectedVersion, out var actor, out var version, out var problem))
        {
            return problem!;
        }

        try
        {
            return Results.Ok(await service.AddNoteAsync(
                new AddQuoteNoteCommand(quoteId, request.Text, version!, actor),
                Context(context),
                cancellationToken));
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> UpdateNoteAsync(
        Guid quoteId,
        Guid noteId,
        [FromBody] UpdateQuoteNoteRequest request,
        HttpContext context,
        QuoteManagementService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 2000)
        {
            return Problem(400, "Note must contain between 1 and 2000 characters.");
        }
        if (!TryMutation(context, request.ExpectedVersion, out var actor, out var version, out var problem))
        {
            return problem!;
        }

        try
        {
            return Results.Ok(await service.UpdateNoteAsync(
                new UpdateQuoteNoteCommand(quoteId, noteId, request.Text, version!, actor),
                Context(context),
                cancellationToken));
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> DeleteNoteAsync(
        Guid quoteId,
        Guid noteId,
        [FromBody] DeleteQuoteNoteRequest request,
        HttpContext context,
        QuoteManagementService service,
        CancellationToken cancellationToken)
    {
        if (!TryMutation(context, request.ExpectedVersion, out var actor, out var version, out var problem))
        {
            return problem!;
        }

        try
        {
            return Results.Ok(await service.DeleteNoteAsync(
                new DeleteQuoteNoteCommand(quoteId, noteId, version!, actor),
                Context(context),
                cancellationToken));
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> GetActivitiesAsync(
        Guid quoteId,
        IQuoteActivityService service,
        CancellationToken cancellationToken)
    {
        var items = await service.GetActivitiesAsync(quoteId, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetEmailStatusAsync(
        Guid quoteId,
        IQuoteEmailQueueService service,
        CancellationToken cancellationToken)
    {
        var status = await service.GetStatusAsync(quoteId, cancellationToken);
        return Results.Ok(status);
    }

    private static async Task<IResult> RetryEmailAsync(
        Guid quoteId,
        [FromBody] RetryEmailRequest request,
        ClaimsPrincipal user,
        IQuoteEmailQueueService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(user, out var actor))
        {
            return Problem(403, "Invalid user identifier.");
        }

        var result = await service.RetryFailedEmailAsync(request.QueueItemId, actor, cancellationToken);
        return result ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ExportCsvAsync(
        HttpContext context,
        IQuoteExportService service,
        CancellationToken cancellationToken)
    {
        if (!TryBuildQuery(context.Request, out var query, out var detail))
        {
            return Problem(400, detail);
        }

        var bytes = await service.ExportCsvAsync(query!, true, cancellationToken);
        return Results.File(bytes, "text/csv; charset=utf-8", $"Quotes_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static async Task<IResult> ExportXlsxAsync(
        HttpContext context,
        IQuoteExportService service,
        CancellationToken cancellationToken)
    {
        if (!TryBuildQuery(context.Request, out var query, out var detail))
        {
            return Problem(400, detail);
        }

        var bytes = await service.ExportXlsxAsync(query!, true, true, cancellationToken);
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Quotes_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    private static async Task<IResult> GetPdfAsync(
        Guid quoteId,
        IQuotePdfGenerator pdfGenerator,
        CancellationToken cancellationToken)
    {
        try
        {
            var pdf = await pdfGenerator.GenerateAsync(quoteId, cancellationToken);
            return Results.File(pdf.PdfBytes, pdf.ContentType, pdf.FileName);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> ArchiveAsync(
        Guid quoteId,
        [FromBody] ArchiveQuoteRequest request,
        HttpContext context,
        IQuoteDeletionService service,
        CancellationToken cancellationToken)
    {
        if (!TryMutation(context, request.ExpectedVersion, out var actor, out var version, out var problem))
        {
            return problem!;
        }

        try
        {
            await service.ArchiveAsync(
                new ArchiveQuoteCommand(quoteId, request.Reason, version!, actor),
                Context(context),
                cancellationToken);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    private static async Task<IResult> PermanentDeleteAsync(
        Guid quoteId,
        [FromBody] PermanentDeleteQuoteRequest request,
        HttpContext context,
        IQuoteDeletionService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context.User, out var actor))
        {
            return Problem(403, "Authenticated subject must be a valid user.");
        }

        try
        {
            await service.HardDeleteAsync(
                new PermanentDeleteQuoteCommand(quoteId, request.Reason, request.ReAuthToken, actor),
                Context(context),
                cancellationToken);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    private static IResult MapException(Exception ex) => ex switch
    {
        QuoteNotFoundException => Results.NotFound(),
        QuoteConcurrencyException => Problem(409, "The quote was changed by another user. Refresh and retry."),
        InvalidOperationException => Problem(409, ex.Message),
        UnauthorizedAccessException => Problem(403, ex.Message),
        ArgumentException => Problem(400, ex.Message),
        _ => Problem(500, ex.Message)
    };

    private static bool TryMutation(
        HttpContext context,
        string? expectedVersion,
        out Guid actor,
        out byte[]? version,
        out IResult? problem)
    {
        SetPrivateNoStore(context.Response);
        actor = Guid.Empty;
        version = null;
        problem = null;
        if (!TryGetUserId(context.User, out actor))
        {
            problem = Problem(403, "Authenticated subject must be a valid user.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(expectedVersion))
        {
            problem = Problem(400, "expectedVersion is required.");
            return false;
        }
        try
        {
            version = Convert.FromBase64String(expectedVersion);
            if (version.Length != 8)
            {
                problem = Problem(400, "expectedVersion must be 8 bytes.");
                return false;
            }
            return true;
        }
        catch (FormatException)
        {
            problem = Problem(400, "expectedVersion must be base64 encoded.");
            return false;
        }
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }

    private static bool TryBuildQuery(HttpRequest request, out AdminQuoteListQuery? query, out string problemDetail)
    {
        query = null;
        problemDetail = string.Empty;
        int page = 1;
        int pageSize = 20;

        if (request.Query.TryGetValue("page", out var pageValues) && int.TryParse(pageValues, out var p)) page = p;
        if (request.Query.TryGetValue("pageSize", out var sizeValues) && int.TryParse(sizeValues, out var s)) pageSize = s;

        QuoteStatus? status = null;
        if (request.Query.TryGetValue("status", out var statusValues) && Enum.TryParse<QuoteStatus>(statusValues, out var st)) status = st;

        Guid? assignedUserId = null;
        if (request.Query.TryGetValue("assignedUserId", out var assigneeValues) && Guid.TryParse(assigneeValues, out var aId)) assignedUserId = aId;

        query = new AdminQuoteListQuery(
            page,
            pageSize,
            status,
            request.Query["search"].FirstOrDefault(),
            null,
            null,
            assignedUserId,
            request.Query.ContainsKey("unassignedOnly"),
            request.Query["productSearch"].FirstOrDefault(),
            request.Query.ContainsKey("newOnly"),
            AdminQuoteSortOrder.Newest);

        return true;
    }

    private static void SetPrivateNoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static QuoteRequestContext Context(HttpContext c) =>
        new(c.Connection.RemoteIpAddress?.ToString(), c.Request.Headers.UserAgent.ToString(), c.TraceIdentifier);

    private static IResult Problem(int statusCode, string detail) => Results.Problem(statusCode: statusCode, detail: detail);

    private static IResult Unavailable() => Results.Problem(statusCode: 503, title: "Quote service unavailable");
    private static IResult UnavailableForQuote(Guid quoteId) => Results.Problem(statusCode: 503, title: "Quote service unavailable");

    private sealed record AssignQuoteRequest(Guid? AssignedToUserId, string? ExpectedVersion);
    private sealed record UnassignQuoteRequest(string? ExpectedVersion);
    private sealed record ChangeQuoteStatusRequest(QuoteStatus Status, string? Note, string? ExpectedVersion);
    private sealed record AddQuoteNoteRequest(string Text, string? ExpectedVersion);
    private sealed record UpdateQuoteNoteRequest(string Text, string? ExpectedVersion);
    private sealed record DeleteQuoteNoteRequest(string? ExpectedVersion);
    private sealed record RetryEmailRequest(Guid QueueItemId);
    private sealed record ArchiveQuoteRequest(string Reason, string? ExpectedVersion);
    private sealed record PermanentDeleteQuoteRequest(string Reason, string ReAuthToken);
}
