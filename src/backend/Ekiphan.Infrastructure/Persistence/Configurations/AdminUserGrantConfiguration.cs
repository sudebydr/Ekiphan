using Ekiphan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class AdminUserGrantConfiguration
    : IEntityTypeConfiguration<AdminUserGrant>
{
    public void Configure(EntityTypeBuilder<AdminUserGrant> builder)
    {
        builder.ToTable("AdminUserPermissions");
        builder.HasKey(item => new { item.AdminUserId, item.Permission });
        builder.Property(item => item.Permission)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(item => item.Permission);
    }
}
