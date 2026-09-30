using System.Security.Claims;
using Ekiphan.Application.MediaImport;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Ekiphan.Api.Media;

internal static class ProductMediaImportEndpoints
{
    public static void MapProductMediaImportEndpoints(this IEndpointRouteBuilder endpoints, bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/product-media-import").RequireRateLimiting("media-upload");
        if (!authenticationConfigured)
        {
            group.MapPost("/preview", Unavailable); group.MapPost("/validate", Unavailable); group.MapPost("/execute", Unavailable);
            group.MapGet("/{batchId:guid}", UnavailableBatch); group.MapGet("/{batchId:guid}/errors", UnavailableBatch);
            group.MapPost("/{batchId:guid}/rollback", UnavailableBatch); return;
        }
        group.RequireAuthorization("MediaImport");
        group.MapPost("/preview", PreviewAsync)
            .WithSummary("ZIP ürün görsellerini önizler")
            .WithDescription("media.import izni gerektirir. multipart/form-data içindeki file alanını güvenli biçimde analiz eder; kalıcı medya oluşturmaz.")
            .Accepts<IFormFile>("multipart/form-data").Produces<ProductMediaImportPreviewDto>(200)
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(413).ProducesProblem(422).ProducesProblem(500)
            .WithMetadata(new RequestSizeLimitAttribute(1537L * 1024 * 1024));
        group.MapPost("/validate", ValidateAsync)
            .WithSummary("ZIP görsellerini ürünlerle eşleştirir")
            .WithDescription("media.import izni gerektirir. Otomatik ve manuel SKU eşleştirmelerini dry-run olarak doğrular; kalıcı kayıt yazmaz.")
            .Produces<ProductMediaImportValidationResultDto>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(422).ProducesProblem(500);
        group.MapPost("/execute", ExecuteAsync)
            .WithSummary("Doğrulanmış ürün görsellerini içe aktarır")
            .WithDescription("media.import izni gerektirir. Tek kullanımlık validation token ile mevcut medya servisini ve ürün-medya ilişkisini kullanır.")
            .Produces<ProductMediaImportExecutionResultDto>(201).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(422).ProducesProblem(500);
        group.MapGet("/{batchId:guid}", DetailAsync)
            .WithSummary("Medya import batch detayını getirir").WithDescription("media.import izni gerektirir.")
            .Produces<ProductMediaImportBatchDetailDto>().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(500);
        group.MapGet("/{batchId:guid}/errors", ErrorsAsync)
            .WithSummary("Medya import hatalarını CSV indirir").WithDescription("media.import izni gerektirir. UTF-8 BOM ile Excel uyumlu CSV döndürür.")
            .Produces(200, contentType: "text/csv").ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(500);
        group.MapPost("/{batchId:guid}/rollback", RollbackAsync).RequireAuthorization("MediaImportRollback")
            .WithSummary("Medya import batch'ini geri alır").WithDescription("media.import.rollback izni gerektirir. Sonradan değiştirilen bağlantıları korur.")
            .Produces<ProductMediaImportRollbackResultDto>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(422).ProducesProblem(500);
    }

    private static async Task<IResult> PreviewAsync(HttpRequest request, IProductMediaImportService service,
        IOptions<ProductMediaImportOptions> options, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType) return Problem(400, "multipart/form-data is required.");
        var form = await request.ReadFormAsync(cancellationToken);
        if (form.Files.Count != 1 || !string.Equals(form.Files[0].Name, "file", StringComparison.Ordinal)) return Problem(400, "Exactly one file field named 'file' is required.");
        var file = form.Files[0]; if (file.Length > options.Value.MaxZipBytes) return Problem(413, "ZIP size limit exceeded.");
        if (!TryUser(request.HttpContext.User, out var userId)) return Problem(403, "A valid admin subject is required.");
        return await Execute(async () => { await using var stream = file.OpenReadStream(); return Results.Ok(await service.PreviewAsync(
            new(stream, Path.GetFileName(file.FileName), file.ContentType, file.Length, userId), cancellationToken)); });
    }
    private static Task<IResult> ValidateAsync(ProductMediaImportValidateCommand command, IProductMediaImportValidationService service, CancellationToken ct) =>
        Execute(async () => Results.Ok(await service.ValidateAsync(command, ct)));
    private static Task<IResult> ExecuteAsync(ProductMediaImportExecuteCommand command, IProductMediaImportExecutionService service, CancellationToken ct) =>
        Execute(async () => Results.Created("/api/admin/product-media-import", await service.ExecuteAsync(command, ct)));
    private static async Task<IResult> DetailAsync(Guid batchId, IProductMediaImportService service, CancellationToken ct)
    { var result = await service.GetBatchAsync(batchId, ct); return result is null ? Results.NotFound() : Results.Ok(result); }
    private static async Task<IResult> ErrorsAsync(Guid batchId, IProductMediaImportService service, CancellationToken ct)
    {
        if (await service.GetBatchAsync(batchId, ct) is null) return Results.NotFound();
        return Results.Stream(stream => service.WriteErrorsCsvAsync(batchId, stream, ct), "text/csv; charset=utf-8", $"product-media-import-{batchId:N}-errors.csv");
    }
    private static Task<IResult> RollbackAsync(Guid batchId, ClaimsPrincipal user, IProductMediaImportRollbackService service, CancellationToken ct)
    { if (!TryUser(user, out var userId)) return Task.FromResult(Problem(403, "A valid admin subject is required.")); return Execute(async () => Results.Ok(await service.RollbackAsync(new(batchId, userId), ct))); }
    private static async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ValidationException ex) { return Problem(422, string.Join(" ", ex.Errors.Select(x => x.ErrorMessage))); }
        catch (ProductMediaImportSecurityException ex) { return Problem(422, ex.Message); }
        catch (ProductMediaImportTokenException ex) { return Problem(409, ex.Message); }
        catch (ProductMediaImportConflictException ex) { return Problem(409, ex.Message); }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (ArgumentException ex) { return Problem(400, ex.Message); }
        catch (InvalidOperationException ex) { return Problem(409, ex.Message); }
    }
    private static bool TryUser(ClaimsPrincipal user, out Guid id) => Guid.TryParse(user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier), out id) && id != Guid.Empty;
    private static IResult Unavailable() => Problem(503, "Product media import is unavailable until authentication is configured.");
    private static IResult UnavailableBatch(Guid batchId) => Unavailable();
    private static IResult Problem(int status, string detail) => Results.Problem(statusCode: status, title: ReasonPhrases.GetReasonPhrase(status), detail: detail);
}
