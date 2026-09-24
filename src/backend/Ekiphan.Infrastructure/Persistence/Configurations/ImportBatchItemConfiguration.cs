using Ekiphan.Domain.DataImport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ImportBatchItemConfiguration : IEntityTypeConfiguration<ImportBatchItem>
{
    public void Configure(EntityTypeBuilder<ImportBatchItem> builder)
    {
        builder.ToTable("ImportBatchItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.ErrorField).HasMaxLength(100);
        builder.Property(x => x.ErrorCode).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(x => new { x.ImportBatchId, x.RowNumber }).IsUnique();
        builder.HasIndex(x => new { x.ImportBatchId, x.Status });
        builder.ToTable(table => table.HasCheckConstraint("CK_ImportBatchItems_RowNumber", "[RowNumber] >= 2"));
    }
}
