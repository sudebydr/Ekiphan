using Ekiphan.Domain.DataImport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ImportRowConfiguration
    : IEntityTypeConfiguration<ImportRow>
{
    public void Configure(EntityTypeBuilder<ImportRow> builder)
    {
        builder.ToTable("ImportRows");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.SheetName).HasMaxLength(150).IsRequired();
        builder.Property(row => row.SKU)
            .HasMaxLength(100)
            .IsUnicode(false);
        builder.Property(row => row.RawPayload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();
        builder.Property(row => row.NormalizedPayload)
            .HasColumnType("nvarchar(max)");
        builder.HasIndex(row => new
        {
            row.ImportJobId,
            row.SheetName,
            row.RowNumber,
        }).IsUnique();
        builder.HasIndex(row => new
        {
            row.ImportJobId,
            row.Status,
            row.RowNumber,
        });
        builder.HasIndex(row => new
        {
            row.SKU,
            row.Status,
        }).HasFilter("[SKU] IS NOT NULL");
        builder.HasMany(row => row.Issues)
            .WithOne()
            .HasForeignKey(issue => issue.ImportRowId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ImportRows_RowNumber",
            "[RowNumber] > 0"));
    }
}
