using Ekiphan.Application.CatalogPdfImport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.Media;

internal static class CatalogPdfImportEndpoints
{
    public static void MapCatalogPdfImportEndpoints(this IEndpointRouteBuilder endpoints, bool authenticationConfigured)
    {
        endpoints.MapGet("/api/catalogs/documents", async (ICatalogPdfImportService service, CancellationToken ct) =>
            Results.Ok(await service.GetPublicDocumentsAsync(ct)));
        var group = endpoints.MapGroup("/api/admin/catalog-pdf-import").RequireRateLimiting("media-upload");
        if (!authenticationConfigured) { group.MapPost("/preview", () => Problem(503, "Catalog PDF import is unavailable until authentication is configured.")); return; }
        group.RequireAuthorization("MediaImport");
        group.MapPost("/preview", async (HttpRequest request, ICatalogPdfImportService service, CancellationToken ct) =>
        {
            if (!request.HasFormContentType) return Problem(400, "multipart/form-data is required.");
            var form = await request.ReadFormAsync(ct);
            if (form.Files.Count != 1 || form.Files[0].Name != "file") return Problem(400, "Exactly one ZIP file field named 'file' is required.");
            var file = form.Files[0];
            await using var content = file.OpenReadStream();
            try { return Results.Ok(await service.PreviewAsync(content, Path.GetFileName(file.FileName), file.Length, ct)); }
            catch (ArgumentException ex) { return Problem(422, ex.Message); }
        }).WithMetadata(new RequestSizeLimitAttribute(2049L * 1024 * 1024));
        group.MapPost("/execute", async (HttpRequest request, ICatalogPdfImportService service, CancellationToken ct) =>
        {
            if (!request.HasFormContentType) return Problem(400, "multipart/form-data is required.");
            var form = await request.ReadFormAsync(ct);
            if (!string.Equals(form["confirmed"], "true", StringComparison.Ordinal) || form.Files.Count != 1 || form.Files[0].Name != "file")
                return Problem(400, "A ZIP file and confirmed=true are required.");
            var file = form.Files[0];
            await using var content = file.OpenReadStream();
            try { return Results.Created("/api/catalogs/documents", await service.ExecuteAsync(content, Path.GetFileName(file.FileName), file.Length, ct)); }
            catch (ArgumentException ex) { return Problem(422, ex.Message); }
            catch (InvalidOperationException ex) { return Problem(409, ex.Message); }
        }).WithMetadata(new RequestSizeLimitAttribute(2049L * 1024 * 1024));
    }
    private static IResult Problem(int status, string detail) => Results.Problem(statusCode: status, title: ReasonPhrases.GetReasonPhrase(status), detail: detail);
}
