using Ekiphan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class AdminSessionConfiguration
    : IEntityTypeConfiguration<AdminSession>
{
    public void Configure(EntityTypeBuilder<AdminSession> builder)
    {
        builder.ToTable("AdminSessions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.LastActivityAt).HasPrecision(0);
        builder.Property(item => item.ExpiresAt).HasPrecision(0);
        builder.Property(item => item.RevokedAt).HasPrecision(0);
        builder.Property(item=>item.DeviceId).HasMaxLength(200);builder.Property(item=>item.DeviceName).HasMaxLength(200);
        builder.Property(item=>item.UserAgent).HasMaxLength(1000);builder.Property(item=>item.Browser).HasMaxLength(100);
        builder.Property(item=>item.OperatingSystem).HasMaxLength(100);builder.Property(item=>item.IpAddress).HasMaxLength(64);
        builder.Property(item=>item.SecurityStampAtCreation).HasMaxLength(64);builder.Property(item=>item.RevokeReason).HasMaxLength(500);
        builder.HasIndex(item=>item.DeviceId);builder.HasIndex(item=>item.ExpiresAt);builder.HasIndex(item=>item.RevokedAt);
        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(item => item.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new
        {
            item.AdminUserId,
            item.ExpiresAt,
        });
        builder.HasIndex(item => new
        {
            item.AdminUserId,
            item.RevokedAt,
        });
    }
}
