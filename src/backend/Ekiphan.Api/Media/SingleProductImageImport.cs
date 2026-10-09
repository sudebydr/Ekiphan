using System.IO.Compression;
using Ekiphan.Application.MediaImport;

namespace Ekiphan.Api.Media;

// Adapt a bounded single-image upload to the existing archive validation/execution pipeline.
public static class SingleProductImageImport
{
    public static async Task<ProductMediaImportExecutionResultDto> ExecuteAsync(IServiceProvider services,
        Stream content, string fileName, Guid owner, CancellationToken ct)
    {
        using var archive = new MemoryStream();
        using (var zip = new ZipArchive(archive, ZipArchiveMode.Create, leaveOpen: true))
        {
            await using var entry = zip.CreateEntry(fileName, CompressionLevel.NoCompression).Open();
            await content.CopyToAsync(entry, ct);
        }
        archive.Position = 0;
        var preview = await services.GetRequiredService<IProductMediaImportService>().PreviewAsync(
            new(archive, "single-product-image.zip", "application/zip", archive.Length, owner), ct);
        var tokens = services.GetRequiredService<IProductMediaImportTokenService>();
        try
        {
            var validation = await services.GetRequiredService<IProductMediaImportValidationService>()
                .ValidateAsync(new(preview.UploadToken, null, new()), ct);
            var file = validation.Files.Single();
            if (file.Status != ProductMediaValidationFileStatus.Valid && file.Status != ProductMediaValidationFileStatus.Duplicate)
                throw new ArgumentException(file.Errors.Any(e => e.Code == "AMBIGUOUS_SKU")
                    ? "Dosya adı birden fazla ürünle eşleşiyor. Lütfen tam ürün SKU'sunu kullanın."
                    : file.Status is ProductMediaValidationFileStatus.Unmatched or ProductMediaValidationFileStatus.Ignored
                        ? "Dosya adındaki SKU ile ürün bulunamadı. Lütfen ürün kodunu kontrol edin."
                        : "Görsel doğrulanamadı: " + string.Join(" ", file.Errors.Select(e => e.Message)));
            return await services.GetRequiredService<IProductMediaImportExecutionService>()
                .ExecuteAsync(new(validation.ValidationToken), ct);
        }
        finally
        {
            if (tokens.TryGetUpload(preview.UploadToken, out var upload))
                await services.GetRequiredService<ITemporaryProductMediaStorage>().DeleteAsync(upload.Archive.ContainerId, CancellationToken.None);
        }
    }
}
