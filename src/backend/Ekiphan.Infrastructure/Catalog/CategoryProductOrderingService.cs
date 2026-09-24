using Ekiphan.Application.Catalog;
using Ekiphan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ekiphan.Infrastructure.Catalog;

public sealed class CategoryProductOrderingService(EkiphanDbContext dbContext) : ICategoryProductOrderingService
{
    public async Task ReorderAsync(
        ReorderCategoryProductsCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == command.CategoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Category was not found.");

        var productIds = command.Items.Select(x => x.ProductId).ToList();
        var products = await dbContext.Products
            .Include(x => x.Categories)
            .Where(x => productIds.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        if (products.Count != command.Items.Count)
        {
            throw new InvalidOperationException(ProductManagementErrorCodes.ProductCategoryReorderInvalid);
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in command.Items)
        {
            var product = products.First(x => x.Id == item.ProductId);
            var pc = product.Categories.SingleOrDefault(c => c.CategoryId == command.CategoryId);
            if (pc is null)
            {
                throw new InvalidOperationException($"Product '{product.SKU}' is not assigned to category '{command.CategoryId}'.");
            }
            pc.SetSortOrder(item.SortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}
