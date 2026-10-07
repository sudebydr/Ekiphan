using System.Security.Claims;
using Ekiphan.Application.DataImport;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Ekiphan.Api.DataImport;

internal static class ProductImportEndpoints
{
    public static void MapProductImportEndpoints(this IEndpointRouteBuilder endpoints, bool authenticationConfigured)
    {
        var group = endpoints.MapGroup("/api/admin/product-import")
            .RequireRateLimiting("import-upload");
        group.MapPost("/preview", PreviewAsync)
            .WithSummary("Ürün import dosyasını önizler")
            .WithDescription("multipart/form-data içindeki tek CSV/XLSX dosyasını güvenli biçimde okur; ilk 20 satırı ve otomatik sütun eşleştirmelerini döndürür.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ProductImportPreviewDto>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(409).ProducesProblem(500)
            .WithMetadata(new RequestSizeLimitAttribute(50L * 1024 * 1024));
        group.MapPost("/validate", ValidateAsync)
            .WithSummary("Ürün importunu dry-run doğrular")
            .WithDescription("Kullanıcı sütun eşleştirmelerini uygular; SKU, referans ve uzunluk kontrollerini yapar fakat ürün kaydetmez.")
            .Produces<ProductImportValidationResultDto>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        group.MapPost("/execute", ExecuteAsync)
            .WithSummary("Doğrulanmış ürün importunu çalıştırır")
            .WithDescription("Süresi dolmamış başarılı validation token ile ürünleri taslak olarak transaction içinde oluşturur.")
            .Produces<ProductImportExecutionResultDto>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        group.MapGet("/{batchId:guid}", DetailAsync)
            .WithSummary("Import batch detayını getirir")
            .WithDescription("Batch durumu, satır sayıları, hata ve rollback uygunluk özetini döndürür.")
            .Produces<ProductImportBatchDetailDto>().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(500);
        group.MapGet("/{batchId:guid}/errors", ErrorsAsync)
            .WithSummary("Import hatalarını CSV indirir")
            .WithDescription("UTF-8 BOM içeren ve Türkçe Excel ile uyumlu hata CSV dosyası üretir.")
            .Produces(200, contentType: "text/csv").ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(500);
        group.MapPost("/{batchId:guid}/rollback", RollbackAsync)
            .RequireAuthorization("ProductsImportRollback")
            .WithSummary("Import batch'ini geri alır")
            .WithDescription("Yalnız bu batch ile eklenen ve importtan sonra değişmemiş ürünleri soft-delete yapar; değişenleri korur.")
            .Produces<ProductImportRollbackResultDto>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
    }

    private static async Task<IResult> PreviewAsync(HttpRequest request, IProductImportService service,
        IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType) return Problem(400, "multipart/form-data biçiminde bir istek gereklidir.");
        var form = await request.ReadFormAsync(cancellationToken);
        if (form.Files.Count != 1) return Problem(400, "Tam olarak bir dosya gereklidir.");
        var file = form.Files[0];
        if (!Allowed(file)) return Problem(400, "Dosya uzantısı ve MIME türü desteklenmiyor.");
        var maximum = configuration.GetValue("ProductImport:MaximumFileSizeBytes", ImportFileReadOptions.DefaultMaximumFileSizeBytes);
        if (file.Length <= 0 || file.Length > maximum) return Problem(400, $"Dosya boyutu 1 ile {maximum} bayt arasında olmalıdır.");
        var userId = Guid.NewGuid();
        return await Execute(async () =>
        {
            await using var stream = file.OpenReadStream();
            return Results.Ok(await service.PreviewAsync(new ProductImportPreviewRequest(stream,
                Path.GetFileName(file.FileName), file.ContentType, file.Length, userId), cancellationToken));
        });
    }

    private static Task<IResult> ValidateAsync(ProductImportValidateCommand command,
        IProductImportValidationService service, CancellationToken cancellationToken) =>
        Execute(async () => Results.Ok(await service.ValidateAsync(command, cancellationToken)));

    private static Task<IResult> ExecuteAsync(ProductImportExecuteCommand command,
        IProductImportExecutionService service, CancellationToken cancellationToken) =>
        Execute(async () => Results.Ok(await service.ExecuteAsync(command, cancellationToken)));

    private static async Task<IResult> DetailAsync(Guid batchId, IProductImportService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetBatchAsync(batchId, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ErrorsAsync(Guid batchId, IProductImportService service,
        CancellationToken cancellationToken)
    {
        if (await service.GetBatchAsync(batchId, cancellationToken) is null) return Results.NotFound();
        return Results.Stream(stream => service.WriteErrorsCsvAsync(batchId, stream, cancellationToken),
            "text/csv; charset=utf-8", $"product-import-{batchId:N}-errors.csv");
    }

    private static Task<IResult> RollbackAsync(Guid batchId, ClaimsPrincipal user,
        IProductImportRollbackService service, CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();
        return Execute(async () => Results.Ok(await service.RollbackAsync(
            new ProductImportRollbackCommand(batchId, userId), cancellationToken)));
    }

    private static async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ValidationException exception) { return Problem(400, string.Join(" ", exception.Errors.Select(x => x.ErrorMessage))); }
        catch (ArgumentException exception) { return Problem(400, exception.Message); }
        catch (ProductImportTokenException exception) { return Problem(409, exception.Message); }
        catch (ProductImportConflictException exception) { return Problem(409, exception.Message); }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (InvalidOperationException exception) { return Problem(409, exception.Message); }
    }

    private static bool Allowed(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? file.ContentType is "text/csv" or "application/csv" or "application/vnd.ms-excel"
            : extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
              file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }
    private static bool TryUser(ClaimsPrincipal user, out Guid id) =>
        Guid.TryParse(user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier), out id) && id != Guid.Empty;
    private static IResult Unavailable() => Problem(503, "Kimlik doğrulama yapılandırılana kadar ürün importu kullanılamaz.");
    private static IResult UnavailableForBatch(Guid batchId) => Unavailable();
    private static IResult Problem(int status, string detail) => Results.Problem(statusCode: status,
        title: ReasonPhrases.GetReasonPhrase(status), detail: detail);
}
