using Ekiphan.Domain.DataImport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ImportJobConfiguration
    : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("ImportJobs");
        builder.HasKey(job => job.Id);
        builder.Property(job => job.OriginalFileName).HasMaxLength(260);
        builder.Property(job => job.SourceSha256Checksum)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(job => job.FailureReason).HasMaxLength(2000);
        builder.HasIndex(job => job.SourceSha256Checksum).IsUnique();
        builder.HasIndex(job => new
        {
            job.Status,
            job.CreatedAt,
        });
        builder.HasIndex(job => new
        {
            job.CreatedByUserId,
            job.CreatedAt,
        });
        builder.HasMany(job => job.Rows)
            .WithOne()
            .HasForeignKey(row => row.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_ImportJobs_RowCounts",
                "[TotalRowCount] >= 0 AND [ValidRowCount] >= 0 AND " +
                "[InvalidRowCount] >= 0 AND [WarningCount] >= 0 AND " +
                "[PublishedRowCount] >= 0");
            table.HasCheckConstraint(
                "CK_ImportJobs_FileSource",
                "([SourceType] = 4 AND [OriginalFileName] IS NULL) OR " +
                "([SourceType] <> 4 AND [OriginalFileName] IS NOT NULL)");
        });
    }
}
