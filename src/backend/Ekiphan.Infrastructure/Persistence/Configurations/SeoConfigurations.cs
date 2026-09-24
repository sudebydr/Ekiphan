using Ekiphan.Domain.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

public sealed class RobotsConfigurationConfiguration : IEntityTypeConfiguration<RobotsConfiguration>
{
    public void Configure(EntityTypeBuilder<RobotsConfiguration> builder)
    {
        builder.ToTable("RobotsConfigurations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EnvironmentName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(20000).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.EnvironmentName).IsUnique();
    }
}

public sealed class RedirectRuleConfiguration : IEntityTypeConfiguration<RedirectRule>
{
    public void Configure(EntityTypeBuilder<RedirectRule> builder)
    {
        builder.ToTable("RedirectRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourcePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NormalizedSourcePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DestinationUrl).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.NormalizedSourcePath, x.IsActive, x.ArchivedAt });
        builder.HasIndex(x => new { x.SourceEntityType, x.SourceEntityId });
    }
}

public sealed class BrokenLinkScanConfiguration : IEntityTypeConfiguration<BrokenLinkScan>
{
    public void Configure(EntityTypeBuilder<BrokenLinkScan> builder)
    {
        builder.ToTable("BrokenLinkScans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);

        builder.HasMany(x => x.Results)
               .WithOne(x => x.Scan)
               .HasForeignKey(x => x.ScanId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.Status, x.RequestedAt });
    }
}

public sealed class BrokenLinkResultConfiguration : IEntityTypeConfiguration<BrokenLinkResult>
{
    public void Configure(EntityTypeBuilder<BrokenLinkResult> builder)
    {
        builder.ToTable("BrokenLinkResults");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.TargetUrl).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => new { x.ScanId, x.ResultStatus });
        builder.HasIndex(x => new { x.SourceEntityType, x.SourceEntityId });
    }
}

public sealed class SeoRevisionConfiguration : IEntityTypeConfiguration<SeoRevision>
{
    public void Configure(EntityTypeBuilder<SeoRevision> builder)
    {
        builder.ToTable("SeoRevisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.SnapshotJson).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.LanguageCode, x.VersionNumber }).IsUnique();
    }
}

public sealed class SeoQualitySnapshotConfiguration : IEntityTypeConfiguration<SeoQualitySnapshot>
{
    public void Configure(EntityTypeBuilder<SeoQualitySnapshot> builder)
    {
        builder.ToTable("SeoQualitySnapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.LanguageCode }).IsUnique();
        builder.HasIndex(x => x.Score);
    }
}

public sealed class SeoImportBatchConfiguration : IEntityTypeConfiguration<SeoImportBatch>
{
    public void Configure(EntityTypeBuilder<SeoImportBatch> builder)
    {
        builder.ToTable("SeoImportBatches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ErrorSummary).HasMaxLength(2000);

        builder.HasMany(x => x.Items)
               .WithOne(x => x.Batch)
               .HasForeignKey(x => x.BatchId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SeoImportBatchItemConfiguration : IEntityTypeConfiguration<SeoImportBatchItem>
{
    public void Configure(EntityTypeBuilder<SeoImportBatchItem> builder)
    {
        builder.ToTable("SeoImportBatchItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
    }
}
