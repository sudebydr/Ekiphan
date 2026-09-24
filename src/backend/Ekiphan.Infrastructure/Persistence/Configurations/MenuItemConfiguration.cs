using Ekiphan.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class MenuItemConfiguration
    : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Url).HasMaxLength(2048).IsRequired();
        builder.Property(item => item.CreatedAt).HasPrecision(0);
        builder.Property(item => item.UpdatedAt).HasPrecision(0);
        builder.HasIndex(item => item.Code).IsUnique();
        builder.HasIndex(item => new
        {
            item.Location,
            item.IsPublished,
            item.SortOrder,
        });
        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(item => item.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Translations)
            .WithOne()
            .HasForeignKey(item => item.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Translations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

