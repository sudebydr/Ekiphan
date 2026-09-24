using Ekiphan.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ReferenceProjectConfiguration : IEntityTypeConfiguration<ReferenceProject>
{
    public void Configure(EntityTypeBuilder<ReferenceProject> builder)
    {
        builder.ToTable("ReferenceProjects");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.WorkflowStatus).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.WorkflowStatus);
        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => x.IsFeatured);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(t => t.ReferenceProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Media)
            .WithOne()
            .HasForeignKey(m => m.ReferenceProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Products)
            .WithOne()
            .HasForeignKey(p => p.ReferenceProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ReferenceProjectTranslationConfiguration : IEntityTypeConfiguration<ReferenceProjectTranslation>
{
    public void Configure(EntityTypeBuilder<ReferenceProjectTranslation> builder)
    {
        builder.ToTable("ReferenceProjectTranslations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(250).IsRequired();

        builder.HasIndex(x => new { x.ReferenceProjectId, x.LanguageCode }).IsUnique();
        builder.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
    }
}

internal sealed class ReferenceProjectMediaConfiguration : IEntityTypeConfiguration<ReferenceProjectMedia>
{
    public void Configure(EntityTypeBuilder<ReferenceProjectMedia> builder)
    {
        builder.ToTable("ReferenceProjectMedia");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ReferenceProjectId, x.MediaAssetId }).IsUnique();
        builder.HasIndex(x => new { x.ReferenceProjectId, x.SortOrder });
    }
}

internal sealed class ReferenceProjectProductConfiguration : IEntityTypeConfiguration<ReferenceProjectProduct>
{
    public void Configure(EntityTypeBuilder<ReferenceProjectProduct> builder)
    {
        builder.ToTable("ReferenceProjectProducts");
        builder.HasKey(x => new { x.ReferenceProjectId, x.ProductId });

        builder.HasIndex(x => new { x.ReferenceProjectId, x.SortOrder });
    }
}

internal sealed class CustomerLogoConfiguration : IEntityTypeConfiguration<CustomerLogo>
{
    public void Configure(EntityTypeBuilder<CustomerLogo> builder)
    {
        builder.ToTable("CustomerLogos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.WebsiteUrl).HasMaxLength(1000);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.SortOrder);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.IsFeatured);
    }
}

internal sealed class ShowroomConfiguration : IEntityTypeConfiguration<Showroom>
{
    public void Configure(EntityTypeBuilder<Showroom> builder)
    {
        builder.ToTable("Showrooms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkflowStatus).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.WorkflowStatus);
        builder.HasIndex(x => x.SortOrder);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(t => t.ShowroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Media)
            .WithOne()
            .HasForeignKey(m => m.ShowroomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Hotspots)
            .WithOne()
            .HasForeignKey(h => h.ShowroomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ShowroomTranslationConfiguration : IEntityTypeConfiguration<ShowroomTranslation>
{
    public void Configure(EntityTypeBuilder<ShowroomTranslation> builder)
    {
        builder.ToTable("ShowroomTranslations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(250).IsRequired();

        builder.HasIndex(x => new { x.ShowroomId, x.LanguageCode }).IsUnique();
        builder.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
    }
}

internal sealed class ShowroomHotspotConfiguration : IEntityTypeConfiguration<ShowroomHotspot>
{
    public void Configure(EntityTypeBuilder<ShowroomHotspot> builder)
    {
        builder.ToTable("ShowroomHotspots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SceneIdentifier).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.ShowroomId);
        builder.HasIndex(x => x.SceneIdentifier);
        builder.HasIndex(x => x.SortOrder);
    }
}

internal sealed class BannerGroupConfiguration : IEntityTypeConfiguration<BannerGroup>
{
    public void Configure(EntityTypeBuilder<BannerGroup> builder)
    {
        builder.ToTable("BannerGroups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Placement);

        builder.HasMany(x => x.Banners)
            .WithOne()
            .HasForeignKey(b => b.BannerGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("Banners");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkflowStatus).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.BannerGroupId);
        builder.HasIndex(x => x.WorkflowStatus);
        builder.HasIndex(x => x.PublishAt);
        builder.HasIndex(x => x.PublishEndAt);
        builder.HasIndex(x => x.SortOrder);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(t => t.BannerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("SiteSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(150).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsPublic);
    }
}

internal sealed class FooterColumnConfiguration : IEntityTypeConfiguration<FooterColumn>
{
    public void Configure(EntityTypeBuilder<FooterColumn> builder)
    {
        builder.ToTable("FooterColumns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TitleTr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.SortOrder);

        builder.HasMany(x => x.Links)
            .WithOne()
            .HasForeignKey(l => l.FooterColumnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FooterLinkConfiguration : IEntityTypeConfiguration<FooterLink>
{
    public void Configure(EntityTypeBuilder<FooterLink> builder)
    {
        builder.ToTable("FooterLinks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LabelTr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.FooterColumnId);
        builder.HasIndex(x => x.SortOrder);
    }
}

internal sealed class CmsRevisionConfiguration : IEntityTypeConfiguration<CmsRevision>
{
    public void Configure(EntityTypeBuilder<CmsRevision> builder)
    {
        builder.ToTable("CmsRevisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SnapshotJson).IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.VersionNumber }).IsUnique();
        builder.HasIndex(x => x.CreatedAt);
    }
}
