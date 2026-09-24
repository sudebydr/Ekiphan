using Ekiphan.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ContentPageConfiguration
    : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> builder)
    {
        builder.ToTable("ContentPages");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasMaxLength(100).IsRequired();
        builder.Property(item => item.PublishedAt).HasPrecision(0);
        builder.Property(item => item.CreatedAt).HasPrecision(0);
        builder.Property(item => item.UpdatedAt).HasPrecision(0);
        builder.HasIndex(item => item.Code).IsUnique();
        builder.HasIndex(item => new { item.Status, item.UpdatedAt });
        builder.HasMany(item => item.Translations)
            .WithOne()
            .HasForeignKey(item => item.ContentPageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

