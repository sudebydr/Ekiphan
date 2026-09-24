using Ekiphan.Domain.DataImport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.OriginalFileHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(2000);
        builder.Property(x => x.RollbackSummary).HasMaxLength(500);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.CreatedByUserId, x.CreatedAt });
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(table => table.HasCheckConstraint("CK_ImportBatches_Counts",
            "[TotalRows] >= 0 AND [SuccessCount] >= 0 AND [ErrorCount] >= 0"));
    }
}
