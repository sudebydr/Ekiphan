using Ekiphan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class AdminUserConfiguration
    : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("AdminUsers");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Email).HasMaxLength(254).IsRequired();
        builder.Property(item => item.NormalizedEmail)
            .HasMaxLength(254)
            .IsRequired();
        builder.Property(item => item.DisplayName)
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(item => item.PasswordHash)
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(item => item.SecurityStamp)
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(item => item.CreatedAt).HasPrecision(0);
        builder.Property(item => item.UpdatedAt).HasPrecision(0);
        builder.Property(item => item.LockoutEnd).HasPrecision(0);
        builder.Property(item => item.LastLoginAt).HasPrecision(0);
        builder.Property(item => item.LastPermissionChangedAt).HasPrecision(0);
        builder.Property(item => item.TwoFactorSecretEncrypted).HasMaxLength(4000);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.HasIndex(item => item.NormalizedEmail).IsUnique();
        builder.HasMany(item => item.Permissions)
            .WithOne()
            .HasForeignKey(item => item.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Permissions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

