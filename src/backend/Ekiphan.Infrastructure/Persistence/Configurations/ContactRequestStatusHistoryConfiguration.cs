using Ekiphan.Domain.Content;
using Ekiphan.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ekiphan.Infrastructure.Persistence.Configurations;

internal sealed class ContactRequestStatusHistoryConfiguration
    : IEntityTypeConfiguration<ContactRequestStatusHistory>
{
    public void Configure(
        EntityTypeBuilder<ContactRequestStatusHistory> builder)
    {
        builder.ToTable("ContactRequestStatusHistory");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.ChangedAt).HasPrecision(0);
        builder.HasIndex(item => new { item.ContactRequestId, item.ChangedAt });
        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(item => item.ChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
