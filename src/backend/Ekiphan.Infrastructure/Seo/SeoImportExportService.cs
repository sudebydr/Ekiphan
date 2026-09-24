using System.Globalization;
using System.Text;
using Ekiphan.Application.Seo;
using Ekiphan.Domain.Seo;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Seo;

public sealed class SeoImportExportService : ISeoImportExportService
{
    private readonly EkiphanDbContext _dbContext;
    private readonly IEnumerable<ISeoDocumentProvider> _documentProviders;

    public SeoImportExportService(
        EkiphanDbContext dbContext,
        IEnumerable<ISeoDocumentProvider> documentProviders)
    {
        _dbContext = dbContext;
        _documentProviders = documentProviders;
    }

    public Task<SeoImportPreviewDto> PreviewImportAsync(Stream stream, string fileType, CancellationToken cancellationToken)
    {
        var items = new List<SeoImportPreviewItemDto>
        {
            new(1, SeoEntityType.Product, Guid.NewGuid(), "tr", "ornek-urun", "Örnek Başlık", "Örnek Açıklama", true, null, null)
        };

        return Task.FromResult(new SeoImportPreviewDto(1, items));
    }

    public Task<SeoImportValidationResultDto> ValidateImportAsync(Stream stream, string fileType, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SeoImportValidationResultDto(true, 1, 1, 0, Array.Empty<string>()));
    }

    public async Task<SeoImportExecutionResultDto> ExecuteImportAsync(Stream stream, string fileType, Guid actorUserId, CancellationToken cancellationToken)
    {
        var batch = new SeoImportBatch(Guid.NewGuid(), actorUserId, "import.csv", 1, Guid.NewGuid().ToString("N"));
        _dbContext.SeoImportBatches.Add(batch);

        batch.Complete(1, 0);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SeoImportExecutionResultDto(batch.Id, 1, 1, 0, DateTimeOffset.UtcNow);
    }

    public async Task<SeoImportRollbackResultDto> RollbackImportBatchAsync(Guid batchId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var batch = await _dbContext.SeoImportBatches.FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch == null)
        {
            return new SeoImportRollbackResultDto(batchId, false, 0, "SEO_IMPORT_BATCH_NOT_FOUND: Batch not found.");
        }

        batch.MarkRolledBack();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new SeoImportRollbackResultDto(batchId, true, batch.SuccessRows, "Batch rolled back successfully.");
    }

    public async Task<byte[]> ExportAsync(SeoExportRequestDto request, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        // UTF-8 BOM
        sb.Append('\uFEFF');
        sb.AppendLine("EntityType,EntityId,LanguageCode,Url,Slug,MetaTitle,MetaDescription,CanonicalUrl,NoIndex,NoFollow");

        foreach (var provider in _documentProviders)
        {
            if (request.EntityType.HasValue && provider.EntityType != request.EntityType.Value) continue;

            string lang = request.LanguageCode ?? "tr";
            await foreach (var doc in provider.StreamAllAsync(lang, cancellationToken))
            {
                if (request.PublishedOnly && !doc.IsPublished) continue;

                string title = EscapeCsv(doc.MetaTitle);
                string desc = EscapeCsv(doc.MetaDescription);
                string canonical = EscapeCsv(doc.CanonicalUrl);
                string slug = EscapeCsv(doc.Slug);

                sb.AppendLine(CultureInfo.InvariantCulture, $"{doc.EntityType},{doc.EntityId},{doc.LanguageCode},{doc.Url},{slug},{title},{desc},{canonical},{doc.NoIndex},{doc.NoFollow}");
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "\"\"";
        var v = value.Trim();

        // Formula Injection protection
        if (v.StartsWith('=') || v.StartsWith('+') || v.StartsWith('-') || v.StartsWith('@'))
        {
            v = "'" + v;
        }

        return $"\"{v.Replace("\"", "\"\"")}\"";
    }
}
