using System.Globalization;
using System.Security.Claims;
using Ekiphan.Api.Quotes;
using Ekiphan.Application.Content;
using Ekiphan.Domain.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Content;

internal static class ContactEndpoints
{
    public static void MapContactEndpoints(
        this IEndpointRouteBuilder endpoints,
        QuoteConsentSettings consentSettings,
        bool authenticationConfigured)
    {
        var publicEndpoint = !string.IsNullOrWhiteSpace(consentSettings.KvkkVersion)
            ? endpoints.MapPost("/api/contact", SubmitAsync)
            : endpoints.MapPost("/api/contact", ConsentUnavailable);
        publicEndpoint.WithMetadata(new RequestSizeLimitAttribute(16 * 1024))
            .RequireRateLimiting("quote-submit");
        endpoints.MapGet("/api/contact-taxonomy", GetPublicTaxonomyAsync)
            .RequireRateLimiting("quote-submit");

        var admin = endpoints.MapGroup("/api/admin/contact-requests")
            .RequireRateLimiting("quote-admin-read");
        if (!authenticationConfigured)
        {
            admin.MapGet("", Unavailable);
            admin.MapGet("/assignees", Unavailable);
            admin.MapGet("/complaints", Unavailable);
            admin.MapGet("/{requestId:guid}", UnavailableForRequest);
            admin.MapPost("/{requestId:guid}/status", UnavailableForRequest);
            admin.MapPost("/{requestId:guid}/assignment", UnavailableForRequest);
            admin.MapPost("/{requestId:guid}/notes", UnavailableForRequest);
            admin.MapGet("/taxonomy", Unavailable);
            admin.MapPost("/reasons", Unavailable);
            admin.MapPut("/reasons/{id:guid}", UnavailableForRequest);
            admin.MapDelete("/reasons/{id:guid}", UnavailableForRequest);
            admin.MapPost("/complaint-categories", Unavailable);
            admin.MapPut("/complaint-categories/{id:guid}", UnavailableForRequest);
            admin.MapDelete("/complaint-categories/{id:guid}", UnavailableForRequest);
            return;
        }

        admin.RequireAuthorization("ContactRead");
        admin.MapGet("", GetAdminAsync);
        admin.MapGet("/assignees", GetAssigneesAsync);
        admin.MapGet("/complaints", GetComplaintsAsync);
        admin.MapGet("/{requestId:guid}", GetDetailAsync);
        admin.MapGet("/taxonomy", GetTaxonomyAsync);
        MapWrite(admin, "/{requestId:guid}/status", ChangeStatusAsync);
        MapWrite(admin, "/{requestId:guid}/assignment", AssignAsync);
        MapWrite(admin, "/{requestId:guid}/notes", AddNoteAsync);
        MapWrite(admin, "/reasons", CreateReasonAsync);
        MapWritePut(admin, "/reasons/{id:guid}", UpdateReasonAsync);
        MapWriteDelete(admin, "/reasons/{id:guid}", DeleteReasonAsync);
        MapWrite(admin, "/complaint-categories", CreateComplaintCategoryAsync);
        MapWritePut(admin, "/complaint-categories/{id:guid}", UpdateComplaintCategoryAsync);
        MapWriteDelete(
            admin, "/complaint-categories/{id:guid}", DeleteComplaintCategoryAsync);
    }

    private static void MapWrite(
        RouteGroupBuilder group,
        string pattern,
        Delegate handler) =>
        group.MapPost(pattern, handler)
            .RequireAuthorization("ContactManage")
            .RequireRateLimiting("quote-admin-write")
            .WithMetadata(new RequestSizeLimitAttribute(16 * 1024));

    private static void MapWritePut(
        RouteGroupBuilder group,
        string pattern,
        Delegate handler) =>
        group.MapPut(pattern, handler)
            .RequireAuthorization("ContactManage")
            .RequireRateLimiting("quote-admin-write")
            .WithMetadata(new RequestSizeLimitAttribute(16 * 1024));

    private static void MapWriteDelete(
        RouteGroupBuilder group,
        string pattern,
        Delegate handler) =>
        group.MapDelete(pattern, handler)
            .RequireAuthorization("ContactManage")
            .RequireRateLimiting("quote-admin-write");

    private static async Task<IResult> GetTaxonomyAsync(
        HttpResponse response,
        IContactTaxonomyService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return Results.Ok(await service.GetAdminAsync(cancellationToken));
    }

    private static async Task<IResult> GetPublicTaxonomyAsync(
        HttpResponse response,
        IContactTaxonomyService service,
        CancellationToken cancellationToken)
    {
        response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await service.GetPublicAsync(cancellationToken));
    }

    private static async Task<IResult> GetComplaintsAsync(
        HttpRequest request,
        HttpResponse response,
        IContactRequestService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        if (!TryInt(request, "page", 1, 1, int.MaxValue, out var page) ||
            !TryInt(request, "pageSize", 20, 1, 100, out var pageSize) ||
            !TryEnum<ContactRequestStatus>(request, "status", out var status))
        {
            return Problem(400, "Complaint filters are invalid.");
        }
        var search = Value(request, "search");
        if (search is { Length: > 100 })
        {
            return Problem(400, "Complaint filters are invalid.");
        }
        return Results.Ok(await service.GetComplaintsAsync(
            new AdminComplaintListQuery(page, pageSize, status, search),
            cancellationToken));
    }

    private static Task<IResult> CreateReasonAsync(
        SaveReasonRequest request,
        HttpResponse response,
        IContactTaxonomyService service,
        CancellationToken cancellationToken) =>
        ExecuteTaxonomyAsync(response, async () =>
        {
            var value = await service.CreateReasonAsync(
                request.ToCommand(), cancellationToken);
            return Results.Created(
                $"/api/admin/contact-requests/reasons/{value.Id}", value);
        });

    private static Task<IResult> UpdateReasonAsync(
        Guid id,
        SaveReasonRequest request,
        HttpResponse response,
        IContactTaxonomyService service,
        CancellationToken cancellationToken) =>
        ExecuteTaxonomyAsync(response, async () =>
        {
            var value = await service.UpdateReasonAsync(
                id, request.ToCommand(), cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static Task<IResult> CreateComplaintCategoryAsync(
        SaveComplaintCategoryRequest request,
        HttpResponse response,
        IContactTaxonomyService service,
        CancellationToken cancellationToken) =>
        ExecuteTaxonomyAsync(response, async () =>
        {
            var value = await service.CreateComplaintCategoryAsync(
                request.ToCommand(), cancellationToken);
            return Results.Created(
                $"/api/admin/contact-requests/complaint-categories/{value.Id}",
                value);
        });

    private static Task<IResult> UpdateComplaintCategoryAsync(
        Guid id,
        SaveComplaintCategoryRequest request,
        HttpResponse response,
        IContactTaxonomyService service,
        CancellationToken cancellationToken) =>
        ExecuteTaxonomyAsync(response, async () =>
        {
            var value = await service.UpdateComplaintCategoryAsync(
                id, request.ToCommand(), cancellationToken);
            return value is null ? Results.NotFound() : Results.Ok(value);
        });

    private static Task<IResult> DeleteReasonAsync(
        Guid id,
        HttpResponse response,
        IContactTaxonomyService service,
        CancellationToken cancellationToken) =>
        ExecuteTaxonomyAsync(response, async () =>
        {
            var deleted = await service.DeleteReasonAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

    private static Task<IResult> DeleteComplaintCategoryAsync(
        Guid id,
        HttpResponse response,
        IContactTaxonomyService service,
        CancellationToken cancellationToken) =>
        ExecuteTaxonomyAsync(response, async () =>
        {
            var deleted = await service.DeleteComplaintCategoryAsync(
                id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

    private static async Task<IResult> ExecuteTaxonomyAsync(
        HttpResponse response,
        Func<Task<IResult>> operation)
    {
        NoStore(response);
        try
        {
            return await operation();
        }
        catch (ContactTaxonomyConflictException exception)
        {
            return Problem(409, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem(400, exception.Message);
        }
    }

    private static async Task<IResult> SubmitAsync(
        SubmitContactCommand command,
        IContactRequestService service,
        QuoteConsentSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.SubmitAsync(
                command, settings.KvkkVersion!, cancellationToken);
            return Results.Accepted(value: result);
        }
        catch (ArgumentException exception)
        {
            return Problem(400, exception.Message);
        }
    }

    private static async Task<IResult> GetAdminAsync(
        HttpRequest request,
        HttpResponse response,
        IContactRequestService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        if (!TryBuildQuery(request, out var query))
        {
            return Problem(400, "Contact filters are invalid.");
        }
        try
        {
            return Results.Ok(await service.GetAdminAsync(query!, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Problem(400, exception.Message);
        }
    }

    private static async Task<IResult> GetDetailAsync(
        Guid requestId,
        HttpResponse response,
        IContactRequestService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        var detail = await service.GetAdminDetailAsync(requestId, cancellationToken);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    }

    private static async Task<IResult> GetAssigneesAsync(
        HttpResponse response,
        IContactRequestService service,
        CancellationToken cancellationToken)
    {
        NoStore(response);
        return Results.Ok(await service.GetAssigneesAsync(cancellationToken));
    }

    private static Task<IResult> ChangeStatusAsync(
        Guid requestId,
        ChangeContactStatusRequest request,
        HttpContext context,
        IContactRequestService service,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return Task.FromResult(Problem(400, "Status is invalid."));
        }
        return MutateAsync(
            context,
            request.ExpectedVersion,
            (actor, version) => service.ChangeStatusAsync(
                new ChangeContactStatusCommand(
                    requestId, request.Status, version, actor),
                cancellationToken));
    }

    private static Task<IResult> AssignAsync(
        Guid requestId,
        AssignContactRequest request,
        HttpContext context,
        IContactRequestService service,
        CancellationToken cancellationToken) =>
        MutateAsync(
            context,
            request.ExpectedVersion,
            (actor, version) => service.AssignAsync(
                new AssignContactCommand(
                    requestId, request.AssignedToUserId, version, actor),
                cancellationToken));

    private static Task<IResult> AddNoteAsync(
        Guid requestId,
        AddContactNoteRequest request,
        HttpContext context,
        IContactRequestService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 2000)
        {
            return Task.FromResult(Problem(
                400,
                "Note must contain between 1 and 2000 characters."));
        }
        return MutateAsync(
            context,
            request.ExpectedVersion,
            (actor, version) => service.AddNoteAsync(
                new AddContactNoteCommand(
                    requestId, request.Text, version, actor),
                cancellationToken));
    }

    private static async Task<IResult> MutateAsync(
        HttpContext context,
        string? expectedVersion,
        Func<Guid, byte[], Task<AdminContactMutationResult>> action)
    {
        NoStore(context.Response);
        if (!TryUserId(context.User, out var actor))
        {
            return Problem(403, "Authenticated subject must be a valid user.");
        }
        if (!TryVersion(expectedVersion, out var version))
        {
            return Problem(400, "expectedVersion must be valid Base64 for 8 bytes.");
        }
        try
        {
            return Results.Ok(await action(actor, version!));
        }
        catch (ContactRequestNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ContactRequestConcurrencyException)
        {
            return Problem(409, "The contact request changed. Refresh and retry.");
        }
        catch (InvalidOperationException exception)
        {
            return Problem(409, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Problem(400, exception.Message);
        }
    }

    private static bool TryBuildQuery(
        HttpRequest request,
        out AdminContactListQuery? result)
    {
        result = null;
        if (!TryInt(request, "page", 1, 1, int.MaxValue, out var page) ||
            !TryInt(request, "pageSize", 20, 1, 100, out var pageSize) ||
            !TryEnum<ContactRequestStatus>(request, "status", out var status) ||
            !TryDate(request, "dateFrom", out var dateFrom) ||
            !TryDate(request, "dateTo", out var dateTo) ||
            !TryBool(request, "newOnly", out var newOnly) ||
            !TryEnum<AdminContactSortOrder>(request, "sort", out var sort))
        {
            return false;
        }
        var search = Value(request, "search");
        if (search is { Length: > 100 }) return false;
        var assigned = Value(request, "assignedUserId");
        var unassigned = string.Equals(
            assigned, "unassigned", StringComparison.OrdinalIgnoreCase);
        Guid? assignedUserId = null;
        if (assigned is not null && !unassigned)
        {
            if (!Guid.TryParse(assigned, out var parsed)) return false;
            assignedUserId = parsed;
        }
        var reason = Value(request, "contactReasonId");
        Guid? contactReasonId = null;
        if (reason is not null)
        {
            if (!Guid.TryParse(reason, out var parsedReason)) return false;
            contactReasonId = parsedReason;
        }
        result = new AdminContactListQuery(
            page, pageSize, status, search, dateFrom, dateTo,
            assignedUserId, unassigned, newOnly, contactReasonId,
            sort ?? AdminContactSortOrder.Newest);
        return true;
    }

    private static string? Value(HttpRequest request, string key) =>
        request.Query.TryGetValue(key, out var value) &&
        !string.IsNullOrWhiteSpace(value)
            ? value.ToString().Trim()
            : null;

    private static bool TryInt(
        HttpRequest request,
        string key,
        int fallback,
        int minimum,
        int maximum,
        out int value)
    {
        value = fallback;
        var raw = Value(request, key);
        return raw is null || int.TryParse(raw, out value) &&
            value >= minimum && value <= maximum;
    }

    private static bool TryBool(
        HttpRequest request,
        string key,
        out bool value)
    {
        value = false;
        var raw = Value(request, key);
        return raw is null || bool.TryParse(raw, out value);
    }

    private static bool TryDate(
        HttpRequest request,
        string key,
        out DateTimeOffset? value)
    {
        value = null;
        var raw = Value(request, key);
        if (raw is null) return true;
        if (!DateTimeOffset.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed)) return false;
        value = parsed;
        return true;
    }

    private static bool TryEnum<T>(
        HttpRequest request,
        string key,
        out T? value) where T : struct, Enum
    {
        value = null;
        var raw = Value(request, key);
        if (raw is null) return true;
        if (int.TryParse(raw, out _) || !Enum.TryParse<T>(raw, true, out var parsed) ||
            !Enum.IsDefined(parsed)) return false;
        value = parsed;
        return true;
    }

    private static bool TryVersion(string? value, out byte[]? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            version = Convert.FromBase64String(value);
            return version.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(
            user.FindFirstValue("sub") ??
            user.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId) && userId != Guid.Empty;

    private static void NoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "private, no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult ConsentUnavailable(SubmitContactCommand command) =>
        Problem(503, "Contact submission is unavailable until consent is configured.");

    private static IResult Unavailable() =>
        Problem(503, "Contact administration is unavailable until authentication is configured.");

    private static IResult UnavailableForRequest(Guid requestId) => Unavailable();

    private static IResult Problem(int status, string detail) =>
        Results.Problem(
            statusCode: status,
            title: ReasonPhrases.GetReasonPhrase(status),
            detail: detail);

    private sealed record ChangeContactStatusRequest(
        ContactRequestStatus Status,
        string ExpectedVersion);

    private sealed record AssignContactRequest(
        Guid? AssignedToUserId,
        string ExpectedVersion);

    private sealed record AddContactNoteRequest(
        string Text,
        string ExpectedVersion);

    private sealed record SaveReasonRequest(
        string Name,
        int SortOrder,
        bool IsActive,
        bool IsComplaintReason)
    {
        public SaveContactReasonCommand ToCommand() =>
            new(Name, SortOrder, IsActive, IsComplaintReason);
    }

    private sealed record SaveComplaintCategoryRequest(
        Guid ContactReasonId,
        string Name,
        int SortOrder,
        bool IsActive)
    {
        public SaveComplaintCategoryCommand ToCommand() =>
            new(ContactReasonId, Name, SortOrder, IsActive);
    }
}