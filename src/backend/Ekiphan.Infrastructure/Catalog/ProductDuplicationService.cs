using Ekiphan.Application.Catalog;
using Ekiphan.Domain.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

public sealed class ProductDuplicationService(
    EkiphanDbContext dbContext,
    IProductRevisionService revisionService)
    : IProductDuplicationService
{
    public async Task<DuplicateProductResultDto> DuplicateAsync(
        DuplicateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var sourceProduct = await dbContext.Products
            .Include(x => x.Categories)
            .Include(x => x.Translations)
            .Include(x => x.Tags)
            .Include(x => x.Variants)
            .SingleOrDefaultAsync(x => x.Id == command.ProductId && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException(ProductManagementErrorCodes.ProductNotFound);

        var normalizedNewSku = command.NewSku.Trim().ToUpperInvariant();
        var skuExists = await dbContext.Products.AsNoTracking()
            .AnyAsync(x => x.SKU == normalizedNewSku && !x.IsDeleted, cancellationToken);
        if (skuExists)
        {
            throw new InvalidOperationException(ProductManagementErrorCodes.ProductSkuDuplicate);
        }

        var newProductId = Guid.NewGuid();
        var newProduct = new Product(newProductId, normalizedNewSku, sourceProduct.BrandId);

        // Copy Categories
        if (command.CopyCategories)
        {
            newProduct.SetCategories(
                sourceProduct.Categories.Select(c => c.CategoryId).ToList(),
                sourceProduct.PrimaryCategoryId);
        }

        // Copy Translations (Generating new unique slugs)
        if (command.CopyTranslations)
        {
            foreach (var tr in sourceProduct.Translations)
            {
                var newSlug = $"{tr.Slug}-copy-{Guid.NewGuid().ToString("N")[..6]}";
                var canonicalUrl = command.CopySeoFields ? tr.CanonicalUrl : null;
                var metaTitle = command.CopySeoFields ? tr.MetaTitle : null;
                var metaDescription = command.CopySeoFields ? tr.MetaDescription : null;

                newProduct.AddTranslation(
                    tr.LanguageCode,
                    $"{tr.Name} (Copy)",
                    newSlug,
                    tr.ShortDescription,
                    tr.LongDescription,
                    metaTitle,
                    metaDescription,
                    canonicalUrl,
                    tr.NoIndex,
                    tr.NoFollow,
                    tr.OpenGraphTitle,
                    tr.OpenGraphDescription,
                    tr.OpenGraphImageMediaId);
            }
        }

        // Copy Tags
        if (command.CopyTags)
        {
            newProduct.SetTags(sourceProduct.Tags.Select(t => t.TagId).ToList());
        }

        newProduct.SetWorkflowStatus(ProductWorkflowStatus.Draft, command.ActorUserId);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Products.Add(newProduct);
        await dbContext.SaveChangesAsync(cancellationToken);

        await revisionService.CreateRevisionAsync(
            newProduct,
            ProductRevisionChangeType.Duplicated,
            command.ActorUserId,
            reason: $"Duplicated from product '{sourceProduct.SKU}' ({sourceProduct.Id}).",
            source: ProductRevisionSource.Manual,
            cancellationToken: cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new DuplicateProductResultDto(
            newProduct.Id,
            newProduct.SKU,
            newProduct.WorkflowStatus);
    }
}
