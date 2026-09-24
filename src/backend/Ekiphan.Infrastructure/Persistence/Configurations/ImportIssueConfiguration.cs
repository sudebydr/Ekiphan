using Ekiphan.Domain.DataImport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ImportIssueConfiguration
    : IEntityTypeConfiguration<ImportIssue>
{
    public void Configure(EntityTypeBuilder<ImportIssue> builder)
    {
        builder.ToTable("ImportIssues");
        builder.HasKey(issue => issue.Id);
        builder.Property(issue => issue.Code)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(issue => issue.Message).HasMaxLength(2000).IsRequired();
        builder.Property(issue => issue.ColumnName).HasMaxLength(150);
        builder.Property(issue => issue.RawValue).HasMaxLength(2000);
        builder.HasIndex(issue => new
        {
            issue.ImportRowId,
            issue.Severity,
            issue.Code,
        });
    }
}
