using Ekiphan.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ProductBulkOperationConfiguration : IEntityTypeConfiguration<ProductBulkOperation>
{
    public void Configure(EntityTypeBuilder<ProductBulkOperation> builder)
    {
        builder.ToTable("ProductBulkOperations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperationType).IsRequired();
        builder.Property(x => x.RequestedByUserId).IsRequired();
        builder.Property(x => x.RequestedAt).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OperationType);
        builder.HasIndex(x => x.RequestedByUserId);
        builder.HasIndex(x => x.RequestedAt);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(item => item.BulkOperationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductBulkOperationItemConfiguration : IEntityTypeConfiguration<ProductBulkOperationItem>
{
    public void Configure(EntityTypeBuilder<ProductBulkOperationItem> builder)
    {
        builder.ToTable("ProductBulkOperationItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BulkOperationId).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.HasIndex(x => x.BulkOperationId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.Status);
    }
}
