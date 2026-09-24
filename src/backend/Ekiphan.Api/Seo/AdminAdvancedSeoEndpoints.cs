using System.Security.Claims;
using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Ekiphan.Api.Seo;

internal static class AdminAdvancedSeoEndpoints
{
    public static void MapAdminAdvancedSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/seo")
            .RequireAuthorization("SeoAccess");

        // Robots
        group.MapGet("/robots", async (IRobotsConfigurationService service, CancellationToken ct) =>
        {
            var result = await service.GetByEnvironmentAsync("Production", ct);
            return Results.Ok(result);
        });

        group.MapPut("/robots", async (UpdateRobotsConfigurationCommand command, ClaimsPrincipal user, IRobotsConfigurationService service, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var result = await service.UpdateAsync(command, userId, ct);
            return Results.Ok(result);
        });

        group.MapPost("/robots/preview", (UpdateRobotsConfigurationCommand command, IRobotsConfigurationService service) =>
        {
            var preview = service.Preview(command);
            return Results.Ok(preview);
        });

        group.MapPost("/robots/reset-default", async (ClaimsPrincipal user, IRobotsConfigurationService service, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var result = await service.ResetToDefaultAsync("Production", userId, ct);
            return Results.Ok(result);
        });

        // Redirects
        group.MapGet("/redirects", async (bool? includeArchived, IRedirectRuleService service, CancellationToken ct) =>
        {
            var list = await service.GetListAsync(includeArchived ?? false, ct);
            return Results.Ok(list);
        });

        group.MapPost("/redirects", async (CreateRedirectRuleCommand command, ClaimsPrincipal user, IRedirectRuleService service, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var result = await service.CreateAsync(command, userId, ct);
            return Results.Created($"/api/admin/seo/redirects/{result.Id}", result);
        });

        group.MapPost("/redirects/validate", async (ValidateRedirectRuleCommand command, IRedirectRuleService service, CancellationToken ct) =>
        {
            var validation = await service.ValidateAsync(command, ct);
            return Results.Ok(validation);
        });

        group.MapPost("/redirects/{id:guid}/archive", async (Guid id, ClaimsPrincipal user, IRedirectRuleService service, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var success = await service.ArchiveAsync(id, userId, ct);
            return success ? Results.NoContent() : Results.NotFound();
        });

        // Broken Links
        group.MapPost("/broken-links/scans", async (BrokenLinkScanRequest request, ClaimsPrincipal user, IBrokenLinkScanner scanner, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var scan = await scanner.StartScanAsync(request, userId, ct);
            return Results.Accepted($"/api/admin/seo/broken-links/scans/{scan.ScanId}", scan);
        });

        group.MapGet("/broken-links/scans/{scanId:guid}", async (Guid scanId, IBrokenLinkScanner scanner, CancellationToken ct) =>
        {
            var scan = await scanner.GetScanStatusAsync(scanId, ct);
            return scan == null ? Results.NotFound() : Results.Ok(scan);
        });

        group.MapGet("/broken-links/scans/{scanId:guid}/results", async (Guid scanId, BrokenLinkResultStatus? status, IBrokenLinkScanner scanner, CancellationToken ct) =>
        {
            var results = await scanner.GetScanResultsAsync(scanId, status, ct);
            return Results.Ok(results);
        });

        // Quality
        group.MapGet("/quality", async (ISeoQualityService service, CancellationToken ct) =>
        {
            var dashboard = await service.GetDashboardAsync(ct);
            return Results.Ok(dashboard);
        });

        group.MapPost("/quality/recalculate", async (ISeoQualityService service, CancellationToken ct) =>
        {
            await service.RecalculateAllAsync(ct);
            return Results.Accepted();
        });

        // Export
        group.MapGet("/export", async ([AsParameters] SeoExportRequestDto request, ISeoImportExportService service, CancellationToken ct) =>
        {
            var fileBytes = await service.ExportAsync(request, ct);
            string contentType = string.Equals(request.Format, "xlsx", StringComparison.OrdinalIgnoreCase)
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/csv";

            return Results.File(fileBytes, contentType, fileDownloadName: $"seo-export.{request.Format.ToLowerInvariant()}");
        });

        // Revisions
        group.MapGet("/{type}/{id:guid}/revisions", async (string type, Guid id, string? lang, ISeoRevisionService service, CancellationToken ct) =>
        {
            if (!Enum.TryParse<SeoEntityType>(type, true, out var entityType)) return Results.BadRequest();
            var list = await service.GetRevisionsAsync(entityType, id, lang ?? "tr", ct);
            return Results.Ok(list);
        });

        group.MapPost("/{type}/{id:guid}/revisions/{revisionId:guid}/restore", async (string type, Guid id, Guid revisionId, RestoreSeoRevisionCommand command, ClaimsPrincipal user, ISeoRevisionService service, CancellationToken ct) =>
        {
            var userId = GetUserId(user);
            var success = await service.RestoreRevisionAsync(revisionId, userId, command.Reason, ct);
            return success ? Results.Ok() : Results.BadRequest("Restore failed.");
        });
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}
